using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Analysis;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Generators;
using RxBlazorV2Generator.Models;
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

        // Extract ObservableModelInfo for razor analysis (needed by razor analyzer)
        var observableModelClasses = observableModelRecords
            .Select(static (records, _) =>
            {
                return records.Select(r => r?.ModelInfo).ToImmutableArray();
            });

        // Report generator-specific diagnostics (RXBG020, RXBG021)
        // These are filtered to avoid duplicates from analyzer
        context.RegisterSourceOutput(observableModelRecords,
            (spc, records) =>
            {
                foreach (var record in records.Where(r => r != null))
                {
                    // Report RXBG020 for unregistered services
                    foreach (var (paramName, _, typeSymbol, location) in record!.UnregisteredServices)
                    {
                        if (location is not null && typeSymbol is not null)
                        {
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
                    foreach (var (diField, serviceScope, location) in record.DiFieldsWithScope)
                    {
                        if (serviceScope is not null && location is not null)
                        {
                            var modelScope = record.ModelInfo.ModelScope;
                            var violation = CheckScopeViolation(modelScope, serviceScope);
                            if (violation is not null)
                            {
                                var diagnostic = Diagnostic.Create(
                                    Diagnostics.DiagnosticDescriptors.DiServiceScopeViolationWarning,
                                    location,
                                    record.ModelInfo.ClassName,
                                    modelScope,
                                    diField.FieldName,
                                    diField.FieldType,
                                    serviceScope);
                                spc.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            });

        // Generate code for observable models
        context.RegisterSourceOutput(observableModelRecords,
            static (spc, records) =>
            {
                foreach (var record in records.Where(r => r != null))
                {
                    ObservableModelCodeGenerator.GenerateObservableModelPartials(spc, record!.ModelInfo);
                }
            });

        // Register syntax provider for Razor code-behind files
        var razorCodeBehindClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => RazorAnalyzer.IsRazorCodeBehindClass(s),
                transform: static (ctx, _) => (ClassDecl: (ClassDeclarationSyntax)ctx.Node, SemanticModel: ctx.SemanticModel))
            .Collect();

        // Register additional text provider for .razor files
        var razorFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".razor"));

        // Combine code-behind classes with razor files and observable models for complete analysis
        var razorCodeBehindRecords = razorCodeBehindClasses
            .Combine(razorFiles.Collect())
            .Combine(observableModelClasses)
            .Select(static (combined, _) =>
            {
                var ((codeBehindClasses, razorFilesList), models) = combined;
                var records = new List<RazorCodeBehindRecord?>();

                // Analyze existing code-behind classes
                foreach (var (classDecl, semanticModel) in codeBehindClasses)
                {
                    // Find matching razor file
                    var className = classDecl.Identifier.ValueText;
                    var razorFile = razorFilesList.FirstOrDefault(f =>
                        System.IO.Path.GetFileNameWithoutExtension(f.Path) == className);

                    var record = RazorCodeBehindRecord.Create(
                        classDecl,
                        semanticModel,
                        razorFile,
                        models);

                    if (record != null)
                    {
                        records.Add(record);
                    }
                }

                // Detect missing code-behind files for .razor files
                var existingClassNames = new HashSet<string>(codeBehindClasses.Select(c => c.ClassDecl.Identifier.ValueText));
                foreach (var razorFile in razorFilesList)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(razorFile.Path);
                    if (!existingClassNames.Contains(fileName))
                    {
                        var record = RazorCodeBehindRecord.CreateFromRazorFile(razorFile, models);
                        if (record != null)
                        {
                            records.Add(record);
                        }
                    }
                }

                return records.ToImmutableArray();
            });

        // Report diagnostics for razor code-behinds (RXBG009, RXBG019)
        context.RegisterSourceOutput(razorCodeBehindRecords,
            static (spc, recordsArray) =>
            {
                foreach (var record in recordsArray.Where(r => r != null))
                {
                    foreach (var diagnostic in record!.Verify())
                    {
                        spc.ReportDiagnostic(diagnostic);
                    }
                }
            });

        // Extract RazorCodeBehindInfo for code generation
        var allRazorCodeBehinds = razorCodeBehindRecords
            .SelectMany(static (recordsArray, _) =>
            {
                return recordsArray.Where(r => r != null).Select(r => r!.CodeBehindInfo).ToImmutableArray();
            });

        // Generate constructors for all Razor code-behind classes (both existing and missing)
        context.RegisterSourceOutput(allRazorCodeBehinds.Combine(msbuildProvider).Combine(observableModelClasses),
            static (spc, combined) =>
            {
                RazorCodeBehindInfo? source = combined.Left.Left;
                GeneratorConfig config = combined.Left.Right;
                ImmutableArray<ObservableModelInfo?> models = combined.Right;

                if (source != null)
                {
                    RazorCodeGenerator.GenerateRazorConstructors(spc, source, models, config.UpdateFrequencyMs);
                }
            });

        // Generate AddObservableModels extension method
        context.RegisterSourceOutput(observableModelRecords.Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (records, config) = combined;
                var nonNullModels = records.Where(r => r != null).Select(r => r!.ModelInfo).ToArray();
                ObservableModelCodeGenerator.GenerateAddObservableModelsExtension(spc, nonNullModels, config.RootNamespace);
                ObservableModelCodeGenerator.GenerateAddGenericObservableModelsExtension(spc, nonNullModels, config.RootNamespace);
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