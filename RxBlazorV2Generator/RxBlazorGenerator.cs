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
using System.Diagnostics;
using System.Linq;

namespace RxBlazorV2Generator;

public class GeneratorConfig
{
    public int UpdateFrequencyMs { get; }
    public string RootNamespace { get; }
    public bool IsDesignTimeBuild { get; }

    public GeneratorConfig(int updateFrequencyMs, string rootNamespace, bool isDesignTimeBuild)
    {
        UpdateFrequencyMs = updateFrequencyMs;
        RootNamespace = rootNamespace;
        IsDesignTimeBuild = isDesignTimeBuild;
    }
}

[Generator]
public class RxBlazorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        /*while (!Debugger.IsAttached)
            Thread.Sleep(500);*/
#endif
        // Read MSBuild properties for configuration
        var msbuildProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.RxBlazorObservableUpdateFrequencyMs",
                    out var updateFrequencyValue);
                var updateFrequency =
                    int.TryParse(updateFrequencyValue, out var frequency) ? frequency : 100; // Default to 100ms

                // Get root namespace from project properties
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                if (string.IsNullOrEmpty(rootNamespace))
                {
                    provider.GlobalOptions.TryGetValue("build_property.AssemblyName", out rootNamespace);
                }

                rootNamespace ??= "Global"; // Fallback if neither is available

                // Detect design-time build
                provider.GlobalOptions.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuildValue);
                var isDesignTimeBuild = string.Equals(designTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);

                return new GeneratorConfig(updateFrequency, rootNamespace, isDesignTimeBuild);
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

                return (records.ToImmutableArray(), compilation);
            });

        // Process records: Extract ComponentInfo now that all records are available
        // This allows looking up referenced model triggers from their records (including cross-assembly)
        var processedRecords = observableModelRecords
            .Select(static (recordsWithCompilation, _) =>
            {
                var (records, compilation) = recordsWithCompilation;
                // Build lookup dictionary by fully qualified type name
                var recordsByTypeName = new Dictionary<string, ObservableModelRecord>();
                foreach (var record in records.Where(r => r != null))
                {
                    recordsByTypeName[record!.ModelInfo.FullyQualifiedName] = record;
                }

                // Extract ComponentInfo for each record that has [ObservableComponent]
                foreach (var record in records.Where(r => r != null))
                {
                    // Check if record needs ComponentInfo (has ObservableComponent attribute)
                    // This is indicated by ComponentInfo being null but model having the attribute
                    // We'll let ExtractComponentInfo check for the attribute
                    var componentInfo = ObservableModelRecord.ExtractComponentInfo(record!, recordsByTypeName, compilation);
                    if (componentInfo != null)
                    {
                        record!.ComponentInfo = componentInfo;
                    }
                }

                return records;
            });

        // Report diagnostics from records during compilation
        // Strategy: Skip analyzer-handled diagnostics (to avoid duplicate code fixes in IDE)
        // but wrap them in RXBG004 generic error for build output (ensures build fails with clear message)
        context.RegisterSourceOutput(processedRecords.Combine(context.CompilationProvider).Combine(msbuildProvider),
            (spc, combined) =>
            {
                var ((records, compilation), config) = combined;
                foreach (var record in records.Where(r => r != null))
                {
                    // Report diagnostics from record (SSOT)
                    foreach (var diagnostic in record!.Verify())
                    {
                        // Check if this diagnostic is handled by the analyzer
                        if (RxBlazorDiagnosticAnalyzer.AllDiagnostics.All(d => d.Id != diagnostic.Id))
                        {
                            // Skip diagnostic reporting during design-time builds
                            if (!config.IsDesignTimeBuild)
                            {
                                // Not DesignTimeBuild, report directly
                                spc.ReportDiagnostic(diagnostic);
                            }
                        }
                    }

                    // Report RXBG050 for unregistered services (with suppression for well-known external services)
                    foreach (var (paramName, _, typeSymbol, location) in record!.UnregisteredServices)
                    {
                        if (location is not null && typeSymbol is not null)
                        {
                            // Check if this is a well-known external service that should be suppressed
                            if (ExternalServiceHelper.ShouldSuppressUnregisteredServiceWarning(typeSymbol, compilation))
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
                                DiagnosticDescriptors.UnregisteredServiceWarning,
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
                                        CheckScopeViolation(record.ModelInfo.ModelScope, tuple.serviceScope!) is not
                                            null)
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
                                DiagnosticDescriptors.DiServiceScopeViolationError,
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
        context.RegisterSourceOutput(processedRecords,
            static (spc, records) =>
            {
                foreach (var record in records.Where(r => r != null && r!.ShouldGenerateCode))
                {
                    ObservableModelCodeGenerator.GenerateObservableModelPartials(spc, record!.ModelInfo);
                }
            });

        // Generate components for models with [ObservableComponent] attribute
        context.RegisterSourceOutput(processedRecords.Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (records, config) = combined;
                foreach (var record in records.Where(r =>
                             r != null && r!.ShouldGenerateCode && r.ComponentInfo != null))
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

                var detection = RazorInheritanceDetector.DetectDirectInheritance(razorFile, content);
                if (detection.HasValue)
                {
                    var (componentName, genericPart) = detection.Value;
                    var location = RazorInheritanceDetector.CreateRazorFileLocation(razorFile, content);

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.DirectObservableComponentInheritanceError,
                        location,
                        componentName,
                        genericPart);

                    spc.ReportDiagnostic(diagnostic);
                }
            });

        // Check for shared model scoping violations (RXBG014)
        // Count component usage in razor files for non-singleton models
        var allRazorFiles = razorFiles.Collect();
        context.RegisterSourceOutput(processedRecords
                .Combine(allRazorFiles),
            static (spc, combined) =>
            {
                var (records, razorFilesList) = combined;

                foreach (var record in records.Where(r =>
                             r != null && r!.ShouldGenerateCode && r.ComponentInfo != null))
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

                        var location = RazorComponentUsageDetector.DetectComponentUsage(
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
                                DiagnosticDescriptors.SharedModelNotSingletonError,
                                location,
                                record.ModelInfo.FullyQualifiedName,
                                modelScope);

                            spc.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            });

        // Check for same-assembly component usage without @page (RXBG061)
        context.RegisterSourceOutput(processedRecords.Combine(allRazorFiles),
            static (spc, combined) =>
            {
                var (records, razorFilesList) = combined;

                // Detect the default layout component (e.g., MainLayout from RouteView)
                // This component is a top-level component by definition and should not trigger RXBG061
                var defaultLayoutComponent = RazorInheritanceDetector.DetectDefaultLayoutComponent(razorFilesList);

                // Only check components we're generating (same-assembly by definition)
                foreach (var record in records.Where(r => r is not null && r!.ComponentInfo is not null))
                {
                    if (record is null || record.ComponentInfo is null)
                    {
                        continue;
                    }

                    var componentClassName = record.ComponentInfo.ComponentClassName;

                    foreach (var razorFile in razorFilesList)
                    {
                        var content = razorFile.GetText();
                        if (content is null)
                        {
                            continue;
                        }

                        var detection = RazorInheritanceDetector.DetectComponentInheritanceWithoutPage(
                            razorFile,
                            content,
                            componentClassName);

                        if (detection.HasValue && !detection.Value.hasPage)
                        {
                            // Get the Razor file name without extension (e.g., "MainLayout" from "MainLayout.razor")
                            var razorFileName = Path.GetFileNameWithoutExtension(razorFile.Path);

                            // Skip the default layout component - it's a top-level component by definition
                            if (!string.IsNullOrEmpty(defaultLayoutComponent) &&
                                razorFileName == defaultLayoutComponent)
                            {
                                continue;
                            }

                            // Check if this component is actually used (rendered as <ComponentName />)
                            // anywhere else in the same assembly. If it's not used in the same assembly,
                            // it's safe because it will be used from another assembly where all generated
                            // code already exists (no compilation order issues).
                            var isUsedInSameAssembly = RazorInheritanceDetector.IsComponentUsedInAssembly(
                                razorFileName,
                                razorFilesList,
                                razorFile);

                            if (!isUsedInSameAssembly)
                            {
                                // Component is defined but not used in this assembly - safe to skip
                                // It will be used from another assembly where compilation order is not an issue
                                continue;
                            }

                            var fileName = Path.GetFileName(razorFile.Path);
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.SameAssemblyComponentCompositionError,
                                detection.Value.location,
                                fileName,
                                componentClassName);
                            spc.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            });

        // Generate code-behind for components that inherit from ObservableComponents
        // Uses simple regex-based property detection that works across assembly boundaries
        // Combine with component records to pass namespace information for proper using directives
        context.RegisterSourceOutput(allRazorFiles.Combine(processedRecords).Combine(context.CompilationProvider),
            static (spc, combined) =>
            {
                var (files, compilation) = combined;
                var (razorFilesList, records) = files;

                // Find all ObservableComponent types in referenced assemblies
                // This accurately detects components by checking inheritance, not just guessing by assembly reference
                var crossAssemblyComponents = compilation.FindCrossAssemblyObservableComponents();

                // Build map of component class names to their namespaces (same-assembly components)
                var componentNamespaces = new Dictionary<string, string>();
                foreach (var record in records.Where(r => r is not null && r!.ComponentInfo is not null))
                {
                    if (record?.ComponentInfo is not null)
                    {
                        var componentName = record.ComponentInfo.ComponentClassName;
                        var componentNamespace = record.ComponentInfo.ComponentNamespace;
                        componentNamespaces[componentName] = componentNamespace;
                    }
                }

                foreach (var razorFile in razorFilesList)
                {
                    var content = razorFile.GetText();
                    if (content is null)
                    {
                        continue;
                    }

                    // Generate Filter() method for components inheriting from ObservableComponents
                    RazorCodeBehindGenerator.GenerateComponentFilterCodeBehind(
                        spc,
                        razorFile,
                        content,
                        componentNamespaces,
                        crossAssemblyComponents);
                }
            });

        // Generate AddObservableModels extension method
        context.RegisterSourceOutput(processedRecords.Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (records, config) = combined;
                var validModels = records.Where(r => r != null && r!.ShouldGenerateCode).Select(r => r!.ModelInfo)
                    .ToArray();
                ObservableModelCodeGenerator.GenerateAddObservableModelsExtension(spc, validModels,
                    config.RootNamespace);
                ObservableModelCodeGenerator.GenerateAddGenericObservableModelsExtension(spc, validModels,
                    config.RootNamespace);
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