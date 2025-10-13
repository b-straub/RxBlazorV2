using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RxBlazorDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor[] AllDiagnostics =
    [
        DiagnosticDescriptors.ObservableModelAnalysisError,
        DiagnosticDescriptors.RazorAnalysisError,
        DiagnosticDescriptors.CodeGenerationError,
        DiagnosticDescriptors.MethodAnalysisWarning,
        DiagnosticDescriptors.RazorFileReadError,
        DiagnosticDescriptors.CircularModelReferenceError,
        DiagnosticDescriptors.InvalidModelReferenceTargetError,
        DiagnosticDescriptors.UnusedModelReferenceError,
        DiagnosticDescriptors.ComponentNotObservableWarning,
        DiagnosticDescriptors.SharedModelNotSingletonError,
        DiagnosticDescriptors.TriggerTypeArgumentsMismatchError,
        DiagnosticDescriptors.CircularTriggerReferenceError,
        DiagnosticDescriptors.GenericArityMismatchError,
        DiagnosticDescriptors.TypeConstraintMismatchError,
        DiagnosticDescriptors.InvalidOpenGenericReferenceError,
        DiagnosticDescriptors.InvalidInitPropertyError,
        DiagnosticDescriptors.DerivedModelReferenceError
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(AllDiagnostics);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeObservableModelClass, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRazorCodeBehindClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeObservableModelClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        
        // Only analyze classes that might be ObservableModel classes
        if (!ObservableModelAnalyzer.IsObservableModelClass(classDecl)) 
            return;

        try
        {
            // Use the analyzer-compatible method to get diagnostics
            var diagnostics = AnalyzeObservableModelForDiagnostics(classDecl, context.SemanticModel, context.Compilation);
            
            // Report all diagnostics found
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
        catch (Exception ex)
        {
            // Report analysis error if something goes wrong
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                classDecl.Identifier.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);
            
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeRazorCodeBehindClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        
        // Only analyze classes that might be Razor code-behind classes
        if (!RazorAnalyzer.IsRazorCodeBehindClass(classDecl, context.SemanticModel)) 
            return;

        try
        {
            // Use the analyzer-compatible method to get diagnostics
            var diagnostics = AnalyzeRazorCodeBehindForDiagnostics(classDecl, context.SemanticModel);
            
            // Report all diagnostics found
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
        catch (Exception ex)
        {
            // Report analysis error if something goes wrong
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.RazorAnalysisError,
                classDecl.Identifier.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);
            
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Analyzer-compatible method to get diagnostics from ObservableModel classes using existing generator logic
    /// </summary>
    private static List<Diagnostic> AnalyzeObservableModelForDiagnostics(ClassDeclarationSyntax classDecl, SemanticModel semanticModel, Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();
        
        try
        {
            if (semanticModel.GetDeclaredSymbol(classDecl) is not { } namedTypeSymbol)
                return diagnostics;

            // Check if class inherits from ObservableModel
            if (!namedTypeSymbol.InheritsFromObservableModel())
                return diagnostics;

            // Extract diagnostics using existing extension methods
            var (modelReferences, modelReferenceDiagnostics) = classDecl.ExtractModelReferencesWithDiagnostics(semanticModel);
            diagnostics.AddRange(modelReferenceDiagnostics);

            var methods = classDecl.CollectMethods();
            var (commandProperties, commandPropertiesDiagnostics) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);
            diagnostics.AddRange(commandPropertiesDiagnostics);

            var (partialProperties, partialPropertyDiagnostics) = classDecl.ExtractPartialPropertiesWithDiagnostics(semanticModel);
            diagnostics.AddRange(partialPropertyDiagnostics);

            // Check for unused model references (RXBG008)
            // Build symbol map for model references
            var modelSymbolMap = new Dictionary<string, ITypeSymbol>();
            foreach (var modelRef in modelReferences)
            {
                var fullTypeName = string.IsNullOrEmpty(modelRef.ReferencedModelNamespace)
                    ? modelRef.ReferencedModelTypeName
                    : $"{modelRef.ReferencedModelNamespace}.{modelRef.ReferencedModelTypeName}";

                var typeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
                if (typeSymbol != null)
                {
                    modelSymbolMap[modelRef.PropertyName] = typeSymbol;
                }
            }

            // Create temporary model info for enhancement analysis
            var tempModelInfo = new Models.ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                new List<Models.PartialPropertyInfo>(),
                commandProperties,
                methods,
                modelReferences,
                "Singleton",
                new List<Models.DIFieldInfo>(),
                new List<string>(),
                null,
                null,
                new List<string>());

            // Enhance model references with command method analysis
            var enhancedModelReferences = tempModelInfo.EnhanceModelReferencesWithCommandAnalysis(
                semanticModel,
                modelSymbolMap);

            // Check for unused model references using SSOT helper
            var unusedDiagnostics = enhancedModelReferences.CreateUnusedModelReferenceDiagnostics(
                namedTypeSymbol,
                classDecl);
            diagnostics.AddRange(unusedDiagnostics);

            // Check for shared model scoping issues using compilation
            var sharedModelDiagnostics = AnalyzeSharedModelScoping(classDecl, semanticModel, compilation);
            diagnostics.AddRange(sharedModelDiagnostics);
        }
        catch (Exception ex)
        {
            // Report diagnostic instead of throwing
            var location = classDecl.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                location,
                classDecl.Identifier.ValueText,
                ex.Message);
            diagnostics.Add(diagnostic);
        }
        
        return diagnostics;
    }

    /// <summary>
    /// Analyzes shared model scoping using compilation information available in syntax node context
    /// </summary>
    private static List<Diagnostic> AnalyzeSharedModelScoping(ClassDeclarationSyntax classDecl, SemanticModel semanticModel, Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();
        
        try
        {
            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol namedTypeSymbol)
                return diagnostics;

            // Get the model scope from attributes
            var modelScope = "Singleton"; // Default scope
            Location? attributeLocation = null;

            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "ObservableModelScopeAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 && 
                        attribute.ConstructorArguments[0].Value is int scopeValue)
                    {
                        modelScope = scopeValue switch
                        {
                            0 => "Singleton",
                            1 => "Scoped",
                            2 => "Transient",
                            _ => "Singleton"
                        };
                    }
                    attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    break;
                }
            }

            // Only check non-singleton models
            if (modelScope != "Singleton")
            {
                // Count how many components use this model by searching the compilation
                var modelUsageCount = CountModelUsageInCompilation(namedTypeSymbol, compilation);
                
                if (modelUsageCount > 1)
                {
                    var location = attributeLocation ?? classDecl.GetLocation();
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.SharedModelNotSingletonError,
                        location,
                        namedTypeSymbol.ToDisplayString(),
                        modelScope);
                    diagnostics.Add(diagnostic);
                }
            }
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                classDecl.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);
            diagnostics.Add(diagnostic);
        }
        
        return diagnostics;
    }

    /// <summary>
    /// Counts how many components in the compilation use this model type.
    /// Note: This approach iterates through compilation symbols which is more efficient
    /// than syntax tree traversal for cross-file analysis in syntax node context.
    /// </summary>
    private static int CountModelUsageInCompilation(INamedTypeSymbol modelType, Compilation compilation)
    {
        var usageCount = 0;
        
        // Get all types in the compilation that inherit from ObservableComponent<T>
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(type => GetObservableComponentBaseModelType(type) != null);
        
        foreach (var componentType in allTypes)
        {
            var baseModelType = GetObservableComponentBaseModelType(componentType);
            if (baseModelType != null && SymbolEqualityComparer.Default.Equals(baseModelType, modelType))
            {
                usageCount++;
            }
        }
        
        return usageCount;
    }

    /// <summary>
    /// Gets the model type from ObservableComponent&lt;T&gt; base class
    /// </summary>
    private static INamedTypeSymbol? GetObservableComponentBaseModelType(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ObservableComponent" && baseType.TypeArguments.Length == 1)
            {
                return baseType.TypeArguments[0] as INamedTypeSymbol;
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    /// <summary>
    /// Analyzer-compatible method to get diagnostics from Razor code-behind classes.
    /// Uses the same detection logic as the generator (SSOT) by calling RazorAnalyzer.GetRazorCodeBehindInfo.
    /// </summary>
    private static List<Diagnostic> AnalyzeRazorCodeBehindForDiagnostics(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var diagnostics = new List<Diagnostic>();

        // Use the same detection logic as the generator
        var razorInfo = RazorAnalyzer.GetRazorCodeBehindInfo(classDecl, semanticModel);

        // Check if we found a diagnostic issue
        if (razorInfo?.HasDiagnosticIssue == true)
        {
            var location = classDecl.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ComponentNotObservableWarning,
                location,
                classDecl.Identifier.Text);

            diagnostics.Add(diagnostic);
        }

        return diagnostics;
    }
}