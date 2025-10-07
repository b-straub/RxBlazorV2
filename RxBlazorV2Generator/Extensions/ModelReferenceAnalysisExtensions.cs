using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class ModelReferenceAnalysisExtensions
{
    public static List<string> AnalyzeModelReferenceUsage(
        this ClassDeclarationSyntax classDecl,
        string referencedModelName,
        SemanticModel semanticModel,
        ITypeSymbol referencedModelType)
    {
        var usedProperties = new HashSet<string>();

        // Use semantic model to find property access patterns
        foreach (var node in classDecl.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
                var symbol = symbolInfo.Symbol;

                if (symbol == null)
                {
                    continue;
                }

                ITypeSymbol? expressionType = null;

                // Handle property access (property is of the model type)
                if (symbol is IPropertySymbol propertySymbol)
                {
                    expressionType = propertySymbol.Type;
                }
                // Handle local variable
                else if (symbol is ILocalSymbol localSymbol)
                {
                    expressionType = localSymbol.Type;
                }
                // Handle parameter
                else if (symbol is IParameterSymbol paramSymbol)
                {
                    expressionType = paramSymbol.Type;
                }
                // Handle field
                else if (symbol is IFieldSymbol fieldSymbol)
                {
                    expressionType = fieldSymbol.Type;
                }

                // Check if the expression type matches the referenced model type
                if (expressionType != null &&
                    SymbolEqualityComparer.Default.Equals(expressionType, referencedModelType))
                {
                    usedProperties.Add(memberAccess.Name.Identifier.ValueText);
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
                allUsedProperties.ToList()));
        }

        return enhancedModelReferences;
    }
}