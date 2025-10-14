using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Analyzers;

/// <summary>
/// Lightweight analyzer for detecting Razor code-behind classes.
/// Analysis logic is delegated to RazorCodeBehindRecord (SSOT pattern).
/// </summary>
public static class RazorAnalyzer
{
    /// <summary>
    /// Predicate for detecting potential Razor code-behind classes (syntax-only).
    /// Used in incremental generator pipeline for initial filtering.
    /// </summary>
    public static bool IsRazorCodeBehindClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.BaseList?.Types.Any(t =>
               {
                   var typeName = t.Type.ToString();
                   return typeName.Contains("ObservableComponent") ||
                          typeName.Contains("ComponentBase") ||
                          typeName.Contains("OwningComponentBase") ||
                          typeName.Contains("LayoutComponentBase");
               }) == true;
    }

    /// <summary>
    /// Semantic check for whether a class is a Razor code-behind class.
    /// Used when semantic model is available for more accurate detection.
    /// </summary>
    public static bool IsRazorCodeBehindClass(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        if (classDecl.BaseList?.Types.Count > 0)
        {
            foreach (var baseType in classDecl.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type);
                if (typeInfo.Type is INamedTypeSymbol baseTypeSymbol)
                {
                    var currentType = baseTypeSymbol;
                    while (currentType != null)
                    {
                        var fullName = currentType.ToDisplayString();
                        if (fullName.Contains("ObservableComponent") ||
                            fullName.Contains("ComponentBase") ||
                            fullName.Contains("OwningComponentBase") ||
                            fullName.Contains("LayoutComponentBase"))
                        {
                            return true;
                        }
                        currentType = currentType.BaseType;
                    }
                }
            }
        }
        return false;
    }
}
