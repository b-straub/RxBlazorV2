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

    public static List<string> ExtractUsingStatements(this ClassDeclarationSyntax classDecl)
    {
        var usingStatements = new List<string>();

        // Find the compilation unit (root) to get using directives
        var compilationUnit = classDecl.SyntaxTree.GetRoot() as CompilationUnitSyntax;
        if (compilationUnit is not null)
        {
            foreach (var usingDirective in compilationUnit.Usings)
            {
                usingStatements.Add(usingDirective.Name?.ToString() ?? string.Empty);
            }
        }

        return usingStatements;
    }
}