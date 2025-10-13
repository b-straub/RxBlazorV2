using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class ModelReferenceAnalysisExtensions
{
    public static List<string> AnalyzeModelReferenceUsage(
        this ClassDeclarationSyntax classDecl,
        INamedTypeSymbol referencedModelSymbol)
    {
        var usedProperties = new HashSet<string>();
        var possibleProperties = referencedModelSymbol
            .GetMembers()
            .Select(n => n as IPropertySymbol)
            .Where(p => p is not null)
            .Select(p => p!.Name)
            .ToList();
        
        // Use semantic model to find property access patterns
        foreach (var node in classDecl.DescendantNodes())
        {
            if (node is IdentifierNameSyntax identifierNameSyntax)
            {
                if (possibleProperties.Contains(identifierNameSyntax.Identifier.ValueText))
                {
                    usedProperties.Add(identifierNameSyntax.Identifier.ValueText);
                }
            }
        }

        return usedProperties.ToList();
    }

    public static List<string> AnalyzeCommandMethodsForModelReferences(
        this ObservableModelInfo modelInfo,
        CommandPropertyInfo command,
        string referencedModelName,
        SemanticModel semanticModel,
        ITypeSymbol referencedModelType)
    {
        var usedProperties = new HashSet<string>();

        // Analyze execute method for model property usage
        if (command.ExecuteMethod != null && modelInfo.Methods.TryGetValue(command.ExecuteMethod, out var executeMethod))
        {
            var executeProps = executeMethod.AnalyzeMethodForModelReferences(semanticModel,
                referencedModelType);
            foreach (var prop in executeProps)
            {
                usedProperties.Add(prop);
            }
        }

        // Analyze canExecute method for model property usage
        if (command.CanExecuteMethod != null && modelInfo.Methods.TryGetValue(command.CanExecuteMethod, out var canExecuteMethod))
        {
            var canExecuteProps = canExecuteMethod.AnalyzeMethodForModelReferences(semanticModel,
                referencedModelType);
            foreach (var prop in canExecuteProps)
            {
                usedProperties.Add(prop);
            }
        }

        return usedProperties.ToList();
    }

    public static List<ModelReferenceInfo> EnhanceModelReferencesWithCommandAnalysis(
        this ObservableModelInfo modelInfo,
        SemanticModel semanticModel,
        Dictionary<string, ITypeSymbol> modelSymbols)
    {
        var enhancedModelReferences = new List<ModelReferenceInfo>();

        foreach (var modelRef in modelInfo.ModelReferences)
        {
            var allUsedProperties = new HashSet<string>(modelRef.UsedProperties);

            // Get the type symbol for this model reference
            if (modelSymbols.TryGetValue(modelRef.PropertyName, out var modelSymbol))
            {
                // Analyze command methods for additional property references
                foreach (var cmd in modelInfo.CommandProperties)
                {
                    var cmdUsedProps = modelInfo.AnalyzeCommandMethodsForModelReferences(
                        cmd,
                        modelRef.ReferencedModelTypeName,
                        semanticModel,
                        modelSymbol);
                    foreach (var prop in cmdUsedProps)
                    {
                        allUsedProperties.Add(prop);
                    }
                }
            }

            enhancedModelReferences.Add(new ModelReferenceInfo(
                modelRef.ReferencedModelTypeName,
                modelRef.ReferencedModelNamespace,
                modelRef.PropertyName,
                allUsedProperties.ToList(),
                modelRef.AttributeLocation));
        }

        return enhancedModelReferences;
    }

    /// <summary>
    /// Analyzes enhanced model references for unused references and creates diagnostics.
    /// This is the single source of truth for UnusedModelReferenceError (RXBG008) detection.
    /// </summary>
    public static List<Diagnostic> CreateUnusedModelReferenceDiagnostics(
        this List<ModelReferenceInfo> enhancedModelReferences,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classDecl)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var modelRef in enhancedModelReferences)
        {
            if (modelRef.UsedProperties.Count == 0)
            {
                var location = modelRef.AttributeLocation ?? classDecl.Identifier.GetLocation();
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.UnusedModelReferenceError,
                    location,
                    classSymbol.Name,
                    modelRef.ReferencedModelTypeName);
                diagnostics.Add(diagnostic);
            }
        }

        return diagnostics;
    }
}