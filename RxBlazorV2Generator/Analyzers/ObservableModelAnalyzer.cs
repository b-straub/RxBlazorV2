using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;

namespace RxBlazorV2Generator.Analyzers;

public static class ObservableModelAnalyzer
{
    public static bool IsObservableModelClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl) 
            return false;
            
        return classDecl.IsObservableModelClass();
    }

    public static (ObservableModelInfo? modelInfo, List<Diagnostic> diagnostics) GetObservableModelClassInfoWithDiagnostics(GeneratorSyntaxContext context, ServiceInfoList? serviceClasses = null, ObservableModelInfo[]? observableModelClasses = null)
    {
        var diagnostics = new List<Diagnostic>();
        
        try
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is not INamedTypeSymbol namedTypeSymbol) 
                return (null, diagnostics);

            // Check if class inherits from ObservableModel
            if (!namedTypeSymbol.InheritsFromObservableModel()) 
                return (null, diagnostics);
            
            // Extract all components using extension methods
            var methods = classDecl.CollectMethods();
            var (partialProperties, partialPropertyDiagnostics) = classDecl.ExtractPartialPropertiesWithDiagnostics(semanticModel);
            var (commandProperties, commandPropertiesDiagnostics) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);
            var (modelReferences, modelReferenceDiagnostics) = classDecl.ExtractModelReferencesWithDiagnostics(semanticModel, serviceClasses);
            var modelScope = classDecl.ExtractModelScopeFromClass(semanticModel);
            var diFields = classDecl.ExtractDIFields(semanticModel, serviceClasses, observableModelClasses);
            var implementedInterfaces = namedTypeSymbol.ExtractObservableModelInterfaces();
            var genericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();
            var typeConstrains = classDecl.ExtractTypeConstrains();
            var usingStatements = classDecl.ExtractUsingStatements();
            var baseModelType = namedTypeSymbol.GetObservableModelBaseType();
            var baseModelTypeName = baseModelType?.ToDisplayString();

            // Add any diagnostics from partial properties analysis
            diagnostics.AddRange(partialPropertyDiagnostics);

            // Add any diagnostics from command properties analysis
            diagnostics.AddRange(commandPropertiesDiagnostics);

            // Add any diagnostics from model reference analysis
            diagnostics.AddRange(modelReferenceDiagnostics);

            var modelInfo = new ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                partialProperties,
                commandProperties,
                methods,
                modelReferences,
                modelScope,
                diFields,
                implementedInterfaces,
                genericTypes,
                typeConstrains,
                usingStatements,
                baseModelTypeName);

            // Build symbol map for model references
            var modelSymbolMap = new Dictionary<string, ITypeSymbol>();
            foreach (var modelRef in modelReferences)
            {
                // Resolve the type symbol from the reference
                var fullTypeName = string.IsNullOrEmpty(modelRef.ReferencedModelNamespace)
                    ? modelRef.ReferencedModelTypeName
                    : $"{modelRef.ReferencedModelNamespace}.{modelRef.ReferencedModelTypeName}";

                var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(fullTypeName);
                if (typeSymbol != null)
                {
                    modelSymbolMap[modelRef.PropertyName] = typeSymbol;
                }
            }

            // Enhance model references with command method analysis
            var enhancedModelReferences = modelInfo.EnhanceModelReferencesWithCommandAnalysis(
                semanticModel,
                modelSymbolMap);

            // Check for model references with no used properties
            foreach (var modelRef in enhancedModelReferences)
            {
                if (modelRef.UsedProperties.Count == 0)
                {
                    var location = modelRef.AttributeLocation ?? classDecl.Identifier.GetLocation();
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.UnusedModelReferenceError,
                        location,
                        namedTypeSymbol.Name,
                        modelRef.ReferencedModelTypeName);
                    diagnostics.Add(diagnostic);
                }
            }

            var finalModelInfo = new ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                partialProperties,
                commandProperties,
                methods,
                enhancedModelReferences,
                modelScope,
                diFields,
                implementedInterfaces,
                genericTypes,
                typeConstrains,
                usingStatements,
                baseModelTypeName);

            return (finalModelInfo, diagnostics);
        }
        catch (Exception ex)
        {
            // Report diagnostic instead of throwing
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var location = classDecl.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                location,
                classDecl.Identifier.ValueText,
                ex.Message);
            diagnostics.Add(diagnostic);
            return (null, diagnostics);
        }
    }

    public static ObservableModelInfo? GetObservableModelClassInfo(GeneratorSyntaxContext context, ServiceInfoList? serviceClasses = null, ObservableModelInfo[]? observableModelClasses = null)
    {
        var (modelInfo, _) = GetObservableModelClassInfoWithDiagnostics(context, serviceClasses, observableModelClasses);
        return modelInfo;
    }

    /// <summary>
    /// Analyzes an ObservableModel class using semantic model from full compilation.
    /// This version should be used in the final RegisterSourceOutput step after combining with CompilationProvider.
    /// </summary>
    public static ObservableModelInfo? GetObservableModelClassInfoFromCompilation(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        Compilation compilation,
        ServiceInfoList? serviceClasses = null)
    {
        try
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
                return null;

            // Check if class inherits from ObservableModel
            if (!namedTypeSymbol.InheritsFromObservableModel())
                return null;

            // Extract all components using extension methods with proper semantic model
            var methods = classDecl.CollectMethods();
            var partialProperties = classDecl.ExtractPartialProperties(semanticModel);
            var (commandProperties, _) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);
            var (modelReferences, _) = classDecl.ExtractModelReferencesWithDiagnostics(semanticModel, serviceClasses);
            var modelScope = classDecl.ExtractModelScopeFromClass(semanticModel);
            var diFields = classDecl.ExtractDIFields(semanticModel, serviceClasses, null);
            var implementedInterfaces = namedTypeSymbol.ExtractObservableModelInterfaces();
            var genericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();
            var typeConstrains = classDecl.ExtractTypeConstrains();
            var usingStatements = classDecl.ExtractUsingStatements();
            var baseModelType = namedTypeSymbol.GetObservableModelBaseType();
            var baseModelTypeName = baseModelType?.ToDisplayString();

            var modelInfo = new ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                partialProperties,
                commandProperties,
                methods,
                modelReferences,
                modelScope,
                diFields,
                implementedInterfaces,
                genericTypes,
                typeConstrains,
                usingStatements,
                baseModelTypeName);

            // Build symbol map for model references
            var modelSymbolMap = new Dictionary<string, ITypeSymbol>();
            foreach (var modelRef in modelReferences)
            {
                // Resolve the type symbol from the reference using compilation
                var fullTypeName = string.IsNullOrEmpty(modelRef.ReferencedModelNamespace)
                    ? modelRef.ReferencedModelTypeName
                    : $"{modelRef.ReferencedModelNamespace}.{modelRef.ReferencedModelTypeName}";

                var typeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
                if (typeSymbol != null)
                {
                    modelSymbolMap[modelRef.PropertyName] = typeSymbol;
                }
            }

            // Enhance model references with command method analysis
            var enhancedModelReferences = modelInfo.EnhanceModelReferencesWithCommandAnalysis(
                semanticModel,
                modelSymbolMap);

            return new ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                partialProperties,
                commandProperties,
                methods,
                enhancedModelReferences,
                modelScope,
                diFields,
                implementedInterfaces,
                genericTypes,
                typeConstrains,
                usingStatements,
                baseModelTypeName);
        }
        catch (Exception)
        {
            // Silently skip models that fail analysis in code generation
            return null;
        }
    }

    public static List<string> GetObservedProperties(ObservableModelInfo modelInfo, CommandPropertyInfo command)
    {
        return modelInfo.GetObservedPropertiesForCommand(command);
    }
    
}