using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class PropertyAnalysisExtensions
{
    public static List<PartialPropertyInfo> ExtractPartialProperties(this ClassDeclarationSyntax classDecl)
    {
        var partialProperties = new List<PartialPropertyInfo>();

        foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (member.Modifiers.Any(SyntaxKind.PartialKeyword) && 
                member.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true &&
                member.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true)
            {
                partialProperties.Add(new PartialPropertyInfo(
                    member.Identifier.ValueText,
                    member.Type!.ToString()));
            }
        }

        return partialProperties;
    }

    public static List<string> AnalyzeMethodForPropertyUsage(this Dictionary<string, MethodDeclarationSyntax> methods, 
        string methodName, 
        ObservableModelInfo modelInfo)
    {
        try
        {
            if (!methods.TryGetValue(methodName, out var method))
            {
                return new List<string>();
            }

            var usedProperties = new HashSet<string>();
            var partialPropertyNames = new HashSet<string>(modelInfo.PartialProperties.Select(p => p.Name));

            // Walk through the method body and find property references
            var descendants = method.DescendantNodes();
            foreach (var node in descendants)
            {
                if (node is IdentifierNameSyntax identifier)
                {
                    var identifierName = identifier.Identifier.ValueText;
                    if (partialPropertyNames.Contains(identifierName))
                    {
                        usedProperties.Add(identifierName);
                    }
                }
                else if (node is MemberAccessExpressionSyntax memberAccess)
                {
                    var memberName = memberAccess.Name.Identifier.ValueText;
                    if (partialPropertyNames.Contains(memberName))
                    {
                        usedProperties.Add(memberName);
                    }
                }
            }

            return usedProperties.ToList();
        }
        catch (Exception)
        {
            // Return empty list on analysis error rather than throwing
            return new List<string>();
        }
    }

    public static List<string> GetObservedPropertiesForCommand(this ObservableModelInfo modelInfo, CommandPropertyInfo command)
    {
        var observedProps = new HashSet<string>();
        
        // Analyze execute method for property usage
        if (command.ExecuteMethod != null)
        {
            var executeProps = modelInfo.Methods.AnalyzeMethodForPropertyUsage(command.ExecuteMethod, modelInfo);
            foreach (var prop in executeProps)
                observedProps.Add(prop);
        }
        
        // Analyze canExecute method for property usage
        if (command.CanExecuteMethod != null)
        {
            var canExecuteProps = modelInfo.Methods.AnalyzeMethodForPropertyUsage(command.CanExecuteMethod, modelInfo);
            foreach (var prop in canExecuteProps)
                observedProps.Add(prop);
        }
        
        return observedProps.ToList();
    }

    public static List<string> AnalyzePropertyModifications(this MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        var modifiedProperties = new HashSet<string>();
        
        try
        {
            // Walk through the method body and find property assignments
            var descendants = method.DescendantNodes();
            foreach (var node in descendants)
            {
                // Look for assignment expressions: property = value
                if (node is AssignmentExpressionSyntax assignment)
                {
                    var leftSide = assignment.Left;
                    
                    // Handle direct property assignment: MyProperty = value
                    if (leftSide is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(identifier.Identifier.ValueText);
                        }
                    }
                    // Handle member access: this.MyProperty = value or model.MyProperty = value
                    else if (leftSide is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(memberAccess.Name.Identifier.ValueText);
                        }
                    }
                }
                // Look for increment/decrement operations: MyProperty++, MyProperty--, ++MyProperty, --MyProperty
                else if (node is PostfixUnaryExpressionSyntax postfixUnary &&
                         (postfixUnary.IsKind(SyntaxKind.PostIncrementExpression) || 
                          postfixUnary.IsKind(SyntaxKind.PostDecrementExpression)))
                {
                    if (postfixUnary.Operand is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(identifier.Identifier.ValueText);
                        }
                    }
                    else if (postfixUnary.Operand is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(memberAccess.Name.Identifier.ValueText);
                        }
                    }
                }
                else if (node is PrefixUnaryExpressionSyntax prefixUnary &&
                         (prefixUnary.IsKind(SyntaxKind.PreIncrementExpression) || 
                          prefixUnary.IsKind(SyntaxKind.PreDecrementExpression)))
                {
                    if (prefixUnary.Operand is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(identifier.Identifier.ValueText);
                        }
                    }
                    else if (prefixUnary.Operand is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(memberAccess.Name.Identifier.ValueText);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Return empty list on analysis error rather than throwing
        }
        
        return modifiedProperties.ToList();
    }
}