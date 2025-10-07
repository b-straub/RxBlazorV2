using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Extensions;

public static class MethodAnalysisExtensions
{
    public static Dictionary<string, MethodDeclarationSyntax> CollectMethods(this ClassDeclarationSyntax classDecl)
    {
        var methods = new Dictionary<string, MethodDeclarationSyntax>();

        foreach (var member in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            methods[member.Identifier.ValueText] = member;
        }

        return methods;
    }

    public static bool HasCancellationTokenParameter(
        this MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        foreach (var param in method.ParameterList.Parameters)
        {
            if (param.Type == null)
            {
                continue;
            }

            var typeInfo = semanticModel.GetTypeInfo(param.Type);
            if (typeInfo.Type != null &&
                typeInfo.Type.Name == "CancellationToken" &&
                typeInfo.Type.ContainingNamespace?.ToDisplayString() == "System.Threading")
            {
                return true;
            }
        }
        return false;
    }

    public static List<string> AnalyzeMethodForModelReferences(
        this MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        ITypeSymbol referencedModelType)
    {
        var usedProperties = new HashSet<string>();

        // Use semantic model to find property access patterns
        foreach (var node in method.DescendantNodes())
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
}