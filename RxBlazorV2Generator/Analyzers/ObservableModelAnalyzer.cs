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

    public static List<string> GetObservedProperties(ObservableModelInfo modelInfo, CommandPropertyInfo command)
    {
        return modelInfo.GetObservedPropertiesForCommand(command);
    }
    
}