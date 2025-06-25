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

    public static bool HasCancellationTokenParameter(this MethodDeclarationSyntax method)
    {
        return method.ParameterList.Parameters
            .Any(p => p.Type?.ToString().Contains("CancellationToken") == true);
    }

    public static List<string> AnalyzeMethodForModelReferences(this MethodDeclarationSyntax method, string referencedModelName)
    {
        var usedProperties = new HashSet<string>();
        
        // Look for usage patterns like "referencedModelName.PropertyName"
        foreach (var node in method.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var expression = memberAccess.Expression.ToString();
                
                // Check for direct reference (e.g., "CounterModel.Counter1")
                if (expression.Equals(referencedModelName, StringComparison.OrdinalIgnoreCase))
                {
                    usedProperties.Add(memberAccess.Name.Identifier.ValueText);
                }
                // Check for property access through the generated property name
                else if (expression.EndsWith($".{referencedModelName}", StringComparison.OrdinalIgnoreCase))
                {
                    usedProperties.Add(memberAccess.Name.Identifier.ValueText);
                }
            }
        }
        
        return usedProperties.ToList();
    }
}