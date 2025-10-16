using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Analysis;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Generators;
using RxBlazorV2Generator.Models;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace RxBlazorV2Generator;

public class GeneratorConfig
{
    public int UpdateFrequencyMs { get; }
    public string RootNamespace { get; }

    public GeneratorConfig(int updateFrequencyMs, string rootNamespace)
    {
        UpdateFrequencyMs = updateFrequencyMs;
        RootNamespace = rootNamespace;
    }
}

[Generator]
public class RxBlazorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Read MSBuild properties for configuration
        var msbuildProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.RxBlazorObservableUpdateFrequencyMs", out var updateFrequencyValue);
                var updateFrequency = int.TryParse(updateFrequencyValue, out var frequency) ? frequency : 100; // Default to 100ms

                // Get root namespace from project properties
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                if (string.IsNullOrEmpty(rootNamespace))
                {
                    provider.GlobalOptions.TryGetValue("build_property.AssemblyName", out rootNamespace);
                }
                rootNamespace ??= "Global"; // Fallback if neither is available

                return new GeneratorConfig(updateFrequency, rootNamespace);
            });
        
        // Analyze service registrations to detect DI services
        var serviceClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => ServiceAnalyzer.IsServiceRegistration(s),
                transform: static (ctx, _) => ServiceAnalyzer.GetServiceClassInfo(ctx))
            .Where(static m => m is not null)
            .Collect();

        // Collect Observable Model class declarations (syntax only, no semantic analysis yet)
        var observableModelSyntax = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => ObservableModelAnalyzer.IsObservableModelClass(s),
                transform: static (ctx, _) => (ClassDecl: (ClassDeclarationSyntax)ctx.Node, Tree: ctx.Node.SyntaxTree))
            .Collect();

        // Combine syntax nodes with compilation and services for semantic analysis
        var observableModelsWithCompilation = observableModelSyntax
            .Combine(context.CompilationProvider)
            .Combine(serviceClasses);

        // Analyze observable models using ObservableModelRecord (single source of truth)
        var observableModelRecords = observableModelsWithCompilation
            .Select(static (combined, _) =>
            {
                var ((classNodes, compilation), services) = combined;

                // Merge service info
                var mergedServices = new ServiceInfoList();
                foreach (var serviceList in services.Where(s => s != null))
                {
                    foreach (var service in serviceList!.Services)
                    {
                        mergedServices.AddService(service);
                    }
                }

                // Analyze each class with proper semantic model from compilation
                var records = new List<ObservableModelRecord?>();
                foreach (var (classDecl, syntaxTree) in classNodes)
                {
                    // Get semantic model for this specific syntax tree from the compilation
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    var record = ObservableModelRecord.Create(
                        classDecl,
                        semanticModel,
                        compilation,
                        mergedServices);

                    records.Add(record);
                }

                return records.ToImmutableArray();
            });
        
        // Report diagnostics from records during compilation
        // Strategy: Skip analyzer-handled diagnostics (to avoid duplicate code fixes in IDE)
        // but wrap them in RXBG004 generic error for build output (ensures build fails with clear message)
        context.RegisterSourceOutput(observableModelRecords.Combine(context.CompilationProvider),
            (spc, combined) =>
            {
                var (records, compilation) = combined;
                foreach (var record in records.Where(r => r != null))
                {
                    // Report diagnostics from record (SSOT)
                    foreach (var diagnostic in record!.Verify())
                    {
                        // Check if this diagnostic is handled by the analyzer
                        if (RxBlazorDiagnosticAnalyzer.AllDiagnostics.Any(d => d.Id == diagnostic.Id))
                        {
                            // Skip reporting the original diagnostic (analyzer handles it in IDE)
                            // But report a generic generator error for build (with diagnostic title and ID)
                            if (diagnostic.Severity == DiagnosticSeverity.Error)
                            {
                                var wrappedDiagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.GeneratorDiagnosticError,
                                    diagnostic.Location,
                                    diagnostic.Descriptor.Title,
                                    diagnostic.Id);

                                spc.ReportDiagnostic(wrappedDiagnostic);
                            }
                        }
                        else
                        {
                            // Not handled by analyzer, report directly
                            spc.ReportDiagnostic(diagnostic);
                        }
                    }

                    // Report RXBG050 for unregistered services (with suppression for well-known external services)
                    foreach (var (paramName, _, typeSymbol, location) in record!.UnregisteredServices)
                    {
                        if (location is not null && typeSymbol is not null)
                        {
                            // Check if this is a well-known external service that should be suppressed
                            if (Diagnostics.ExternalServiceHelper.ShouldSuppressUnregisteredServiceWarning(typeSymbol, compilation))
                            {
                                continue;
                            }

                            var simpleTypeName = typeSymbol.Name;
                            var isInterface = typeSymbol.TypeKind == TypeKind.Interface;
                            var registrationExample = isInterface
                                ? $"'services.AddScoped<{simpleTypeName}, YourImplementation>()'"
                                : $"'services.AddScoped<{simpleTypeName}>()' or 'services.AddScoped<IYourInterface, {simpleTypeName}>()'";

                            var properties = ImmutableDictionary.CreateRange(new[]
                            {
                                new KeyValuePair<string, string?>("TypeName", typeSymbol.ToDisplayString()),
                                new KeyValuePair<string, string?>("ParameterName", paramName)
                            });

                            var diagnostic = Diagnostic.Create(
                                Diagnostics.DiagnosticDescriptors.UnregisteredServiceWarning,
                                location,
                                properties,
                                paramName,
                                simpleTypeName,
                                registrationExample);
                            spc.ReportDiagnostic(diagnostic);
                        }
                    }

                    // Report RXBG021 for DI scope violations
                    // First, calculate the minimum required scope for ALL violations in this class
                    var violatingFields = record.DiFieldsWithScope
                        .Where(tuple => tuple.serviceScope is not null && tuple.location is not null &&
                                       CheckScopeViolation(record.ModelInfo.ModelScope, tuple.serviceScope!) is not null)
                        .ToList();

                    if (violatingFields.Any())
                    {
                        var modelScope = record.ModelInfo.ModelScope;

                        // Calculate minimum required scope based on all service scopes
                        var hasTransient = violatingFields.Any(f => f.serviceScope == "Transient");
                        var hasScoped = violatingFields.Any(f => f.serviceScope == "Scoped");

                        string requiredScope;
                        if (hasTransient)
                        {
                            requiredScope = "Transient";
                        }
                        else if (hasScoped)
                        {
                            requiredScope = "Scoped";
                        }
                        else
                        {
                            requiredScope = "Singleton";
                        }

                        // Report diagnostic for each violating field, but include the overall required scope in properties
                        foreach (var (diField, serviceScope, location) in violatingFields)
                        {
                            var properties = ImmutableDictionary.CreateRange(new[]
                            {
                                new KeyValuePair<string, string?>("RequiredScope", requiredScope),
                                new KeyValuePair<string, string?>("ClassName", record.ModelInfo.ClassName)
                            });

                            var diagnostic = Diagnostic.Create(
                                Diagnostics.DiagnosticDescriptors.DiServiceScopeViolationError,
                                location,
                                properties,
                                record.ModelInfo.ClassName,
                                modelScope,
                                diField.FieldName,
                                diField.FieldType,
                                serviceScope);
                            spc.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            });

        // Generate code for observable models
        context.RegisterSourceOutput(observableModelRecords,
            static (spc, records) =>
            {
                foreach (var record in records.Where(r => r != null && r!.ShouldGenerateCode))
                {
                    ObservableModelCodeGenerator.GenerateObservableModelPartials(spc, record!.ModelInfo);
                }
            });

        // Generate components for models with [ObservableComponent] attribute
        context.RegisterSourceOutput(observableModelRecords.Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (records, config) = combined;
                foreach (var record in records.Where(r => r != null && r!.ShouldGenerateCode && r.ComponentInfo != null))
                {
                    ComponentCodeGenerator.GenerateComponent(spc, record!.ComponentInfo!, config.UpdateFrequencyMs);
                }
            });

        // Detect and report direct @inherits ObservableComponent usage in razor files
        var razorFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

        context.RegisterSourceOutput(razorFiles,
            static (spc, razorFile) =>
            {
                var content = razorFile.GetText();
                if (content is null)
                {
                    return;
                }

                var detection = Analyzers.RazorInheritanceDetector.DetectDirectInheritance(razorFile, content);
                if (detection.HasValue)
                {
                    var (componentName, genericPart) = detection.Value;
                    var location = Analyzers.RazorInheritanceDetector.CreateRazorFileLocation(razorFile, content);

                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.DirectObservableComponentInheritanceError,
                        location,
                        componentName,
                        genericPart);

                    spc.ReportDiagnostic(diagnostic);
                }
            });

        // Check for shared model scoping violations (RXBG014)
        // Count component usage in razor files for non-singleton models
        var allRazorFiles = razorFiles.Collect();
        context.RegisterSourceOutput(observableModelRecords.Combine(allRazorFiles),
            static (spc, combined) =>
            {
                var (records, razorFilesList) = combined;

                foreach (var record in records.Where(r => r != null && r!.ShouldGenerateCode && r.ComponentInfo != null))
                {
                    if (record is null || record.ComponentInfo is null)
                    {
                        continue;
                    }

                    var modelScope = record.ModelInfo.ModelScope;

                    // Only check non-singleton models
                    if (modelScope == "Singleton")
                    {
                        continue;
                    }

                    var componentClassName = record.ComponentInfo.ComponentClassName;
                    var usageLocations = new List<Location>();

                    // Count usages across all razor files
                    foreach (var razorFile in razorFilesList)
                    {
                        var content = razorFile.GetText();
                        if (content is null)
                        {
                            continue;
                        }

                        var location = Analyzers.RazorComponentUsageDetector.DetectComponentUsage(
                            razorFile,
                            content,
                            componentClassName);

                        if (location is not null)
                        {
                            usageLocations.Add(location);
                        }
                    }

                    // Report diagnostic if model is shared (used in multiple components)
                    if (usageLocations.Count > 1)
                    {
                        // Report diagnostic at ALL usage locations so user sees error in every file
                        foreach (var location in usageLocations)
                        {
                            var diagnostic = Diagnostic.Create(
                                Diagnostics.DiagnosticDescriptors.SharedModelNotSingletonError,
                                location,
                                record.ModelInfo.FullyQualifiedName,
                                modelScope);

                            spc.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            });

        // Generate AddObservableModels extension method
        context.RegisterSourceOutput(observableModelRecords.Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (records, config) = combined;
                var validModels = records.Where(r => r != null && r!.ShouldGenerateCode).Select(r => r!.ModelInfo).ToArray();
                ObservableModelCodeGenerator.GenerateAddObservableModelsExtension(spc, validModels, config.RootNamespace);
                ObservableModelCodeGenerator.GenerateAddGenericObservableModelsExtension(spc, validModels, config.RootNamespace);
            });
    }

    /// <summary>
    /// Checks if there's a DI scope violation between model scope and service scope.
    /// Returns a message describing the violation, or null if no violation.
    ///
    /// DI Scoping Rules:
    /// - Singleton can only inject Singleton
    /// - Scoped can inject Singleton or Scoped
    /// - Transient can inject anything
    /// </summary>
    private static string? CheckScopeViolation(string modelScope, string serviceScope)
    {
        if (modelScope == "Singleton" && serviceScope != "Singleton")
        {
            return $"Singleton services cannot depend on {serviceScope} services (captive dependency)";
        }

        if (modelScope == "Scoped" && serviceScope == "Transient")
        {
            return $"Scoped services should not depend on Transient services (may cause issues with disposal)";
        }

        return null;
    }
}