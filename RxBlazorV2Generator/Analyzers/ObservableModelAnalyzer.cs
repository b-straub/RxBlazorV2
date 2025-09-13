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

            var attributes = namedTypeSymbol.GetAttributes();
            
            // Extract all components using extension methods
            var methods = classDecl.CollectMethods();
            var partialProperties = classDecl.ExtractPartialProperties(semanticModel);
            var (commandProperties, commandPropertiesDiagnostics) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);
            var (modelReferences, modelReferenceDiagnostics) = classDecl.ExtractModelReferencesWithDiagnostics(semanticModel, serviceClasses);
            var modelScope = classDecl.ExtractModelScopeFromClass(semanticModel);
            var diFields = classDecl.ExtractDIFields(semanticModel, serviceClasses, observableModelClasses);
            var implementedInterfaces = namedTypeSymbol.ExtractObservableModelInterfaces();
            var genericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();
            var typeConstrains = classDecl.ExtractTypeConstrains();
            
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
                typeConstrains);

            // Enhance model references with command method analysis
            var enhancedModelReferences = modelInfo.EnhanceModelReferencesWithCommandAnalysis();

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
                typeConstrains);

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

    public static List<string> GetObservedProperties(ObservableModelInfo modelInfo, CommandPropertyInfo command)
    {
        return modelInfo.GetObservedPropertiesForCommand(command);
    }
    
}