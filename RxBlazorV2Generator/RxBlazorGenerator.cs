using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Generators;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;

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

        // Analyze observable models and collect ObservableModelInfo for use by other generators
        var observableModelClasses = observableModelsWithCompilation
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
                var models = new List<ObservableModelInfo?>();
                foreach (var (classDecl, syntaxTree) in classNodes)
                {
                    // Get semantic model for this specific syntax tree from the compilation
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    var modelInfo = ObservableModelAnalyzer.GetObservableModelClassInfoFromCompilation(
                        classDecl,
                        semanticModel,
                        compilation,
                        mergedServices);

                    models.Add(modelInfo);
                }

                return models.ToImmutableArray();
            });

        // Report RXBG020 (UnregisteredServiceWarning) and RXBG021 (DiServiceScopeViolationWarning) diagnostics
        // NOTE: Following SSOT pattern - these diagnostics are reported ONLY by the generator, not the analyzer
        context.RegisterSourceOutput(observableModelsWithCompilation,
            (spc, combined) =>
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

                // Analyze each class for DI diagnostics
                foreach (var (classDecl, syntaxTree) in classNodes)
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

                    if (classSymbol is null || !classSymbol.InheritsFromObservableModel())
                        continue;

                    // Extract DI dependencies to check for unregistered services and scope violations
                    var (modelReferences, diFields, unregisteredServices, diFieldsWithScope) = classDecl.ExtractPartialConstructorDependencies(semanticModel, mergedServices);

                    // Report RXBG020 for unregistered services
                    foreach (var (paramName, _, typeSymbol, location) in unregisteredServices)
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
                    var modelScope = classDecl.ExtractModelScopeFromClass(semanticModel);
                    foreach (var (diField, serviceScope, location) in diFieldsWithScope)
                    {
                        if (serviceScope is not null && location is not null)
                        {
                            var violation = CheckScopeViolation(modelScope, serviceScope);
                            if (violation is not null)
                            {
                                var diagnostic = Diagnostic.Create(
                                    Diagnostics.DiagnosticDescriptors.DiServiceScopeViolationWarning,
                                    location,
                                    classSymbol.Name,
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
        context.RegisterSourceOutput(observableModelClasses,
            static (spc, models) =>
            {
                foreach (var modelInfo in models.Where(m => m != null))
                {
                    ObservableModelCodeGenerator.GenerateObservableModelPartials(spc, modelInfo!);
                }
            });

        // Register syntax provider for Razor code-behind files
        // Store both the info and the context for later RXBG019 diagnostic checking
        var razorCodeBehindClassesWithContext = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => RazorAnalyzer.IsRazorCodeBehindClass(s),
                transform: static (ctx, _) => (Info: RazorAnalyzer.GetRazorCodeBehindInfo(ctx), ClassDecl: (ClassDeclarationSyntax)ctx.Node, SemanticModel: ctx.SemanticModel))
            .Where(static m => m.Info is not null);

        // Extract just the info for compatibility with existing code
        var razorCodeBehindClasses = razorCodeBehindClassesWithContext
            .Select(static (item, _) => item.Info);

        // Register additional text provider for .razor files
        var razorFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".razor"));

        // Combine razor files with code-behind classes and observable models
        var combinedRazorInfo = razorCodeBehindClasses
            .Combine(razorFiles.Collect())
            .Combine(observableModelClasses)
            .Select(static (combined, _) =>
            {
                try
                {
                    return RazorAnalyzer.AnalyzeRazorWithAdditionalTexts(combined.Left.Left, combined.Left.Right, combined.Right);
                }
                catch (Exception)
                {
                    // Log error but don't throw - return null to skip this item
                    return null;
                }
            });

        // Detect missing code-behind files for .razor files that use ObservableComponent
        var missingCodeBehinds = razorFiles.Collect()
            .Combine(razorCodeBehindClasses.Collect())
            .Combine(observableModelClasses)
            .SelectMany(static (combined, _) =>
            {
                var ((razorFilesList, codeBehindsList), models) = combined;
                return RazorAnalyzer.DetectMissingCodeBehindFiles(razorFilesList, codeBehindsList, models);
            });

        // Merge existing and missing code-behind infos
        var allRazorCodeBehinds = combinedRazorInfo
            .Collect()
            .Combine(missingCodeBehinds.Collect())
            .SelectMany(static (combined, _) =>
            {
                var (existing, missing) = combined;
                var all = new List<RazorCodeBehindInfo?>();
                all.AddRange(existing);
                all.AddRange(missing);
                return all;
            });

        // Report RXBG009 diagnostics for razor files with @inject ObservableModel but no ObservableComponent inheritance
        context.RegisterSourceOutput(missingCodeBehinds.Collect().Combine(razorFiles.Collect()),
            static (spc, combined) =>
            {
                var (codeBehindInfos, allRazorFiles) = combined;

                foreach (var info in codeBehindInfos.Where(i => i != null && i.HasDiagnosticIssue))
                {
                    // Find the corresponding .razor file to get its location
                    var razorFile = allRazorFiles.FirstOrDefault(f =>
                        System.IO.Path.GetFileNameWithoutExtension(f.Path) == info!.ClassName);

                    if (razorFile != null)
                    {
                        var location = Location.Create(
                            razorFile.Path,
                            Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(0, 0),
                            new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                                new Microsoft.CodeAnalysis.Text.LinePosition(0, 0),
                                new Microsoft.CodeAnalysis.Text.LinePosition(0, 0)));

                        var diagnostic = Diagnostic.Create(
                            Diagnostics.DiagnosticDescriptors.ComponentNotObservableWarning,
                            location,
                            info!.ClassName);

                        spc.ReportDiagnostic(diagnostic);
                    }
                }
            });

        // Check for RXBG019: .razor file inheritance mismatch
        var razorInheritanceChecks = razorCodeBehindClassesWithContext
            .Combine(razorFiles.Collect())
            .Select(static (combined, _) =>
            {
                var (codeBehindContext, allRazorFiles) = combined;

                // Find the corresponding .razor file
                var razorFile = allRazorFiles.FirstOrDefault(f =>
                    System.IO.Path.GetFileNameWithoutExtension(f.Path) == codeBehindContext.Info?.ClassName);

                if (razorFile == null || codeBehindContext.Info == null)
                {
                    return (ClassDecl: codeBehindContext.ClassDecl, SemanticModel: codeBehindContext.SemanticModel, RazorFile: (AdditionalText?)null, ClassName: codeBehindContext.Info?.ClassName);
                }

                return (ClassDecl: codeBehindContext.ClassDecl, SemanticModel: codeBehindContext.SemanticModel, RazorFile: (AdditionalText?)razorFile, ClassName: codeBehindContext.Info.ClassName);
            });

        // Report RXBG019 diagnostics
        context.RegisterSourceOutput(razorInheritanceChecks,
            static (spc, checkInfo) =>
            {
                if (checkInfo.RazorFile != null && checkInfo.ClassDecl != null && checkInfo.SemanticModel != null)
                {
                    var (hasMatch, expectedInherits) = RazorAnalyzer.CheckRazorInheritanceMatch(
                        checkInfo.ClassDecl,
                        checkInfo.SemanticModel,
                        checkInfo.RazorFile);

                    if (!hasMatch && expectedInherits != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            Diagnostics.DiagnosticDescriptors.RazorInheritanceMismatchWarning,
                            checkInfo.ClassDecl.Identifier.GetLocation(),
                            checkInfo.ClassName,
                            expectedInherits);

                        spc.ReportDiagnostic(diagnostic);
                    }
                }
            });

        // Generate constructors for all Razor code-behind classes (both existing and missing)
        context.RegisterSourceOutput(allRazorCodeBehinds.Combine(msbuildProvider).Combine(observableModelClasses),
            static (spc, combined) =>
            {
                var ((source, config), models) = combined;
                if (source != null)
                {
                    RazorCodeGenerator.GenerateRazorConstructors(spc, source, models, config.UpdateFrequencyMs);
                }
            });

        // Generate AddObservableModels extension method
        context.RegisterSourceOutput(observableModelClasses.Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (models, config) = combined;
                var nonNullModels = models.Where(m => m != null).Select(x => x!).ToArray();
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