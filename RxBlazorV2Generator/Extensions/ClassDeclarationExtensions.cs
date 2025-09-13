using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Extensions;

public static class ClassDeclarationExtensions
{
    public static string ExtractTypeConstrains(this ClassDeclarationSyntax classDecl)
    {
        var typeConstrains = string.Empty;

        foreach (var constraint in classDecl.ConstraintClauses)
        {
            typeConstrains += constraint.ToFullString();
        }
        
        typeConstrains = typeConstrains.TrimEnd();
        return typeConstrains;
    }
}