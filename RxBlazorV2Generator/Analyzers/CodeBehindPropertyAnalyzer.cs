using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Analyzers;

/// <summary>
/// Analyzes .razor.cs code-behind files for Model property usage.
/// Uses Roslyn semantic analysis to accurately detect Model.PropertyName patterns.
/// </summary>
public static class CodeBehindPropertyAnalyzer
{
    /// <summary>
    /// Analyzes a compilation to find all Model property usages in code-behind classes
    /// that inherit from Observable components.
    /// Returns dictionary mapping component class name to list of used property chains.
    /// UNIFIED APPROACH: Uses GeneratorContext as single source of truth.
    /// </summary>
    public static Dictionary<string, HashSet<string>> AnalyzeCodeBehindPropertyUsage(
        Compilation compilation,
        GeneratorContext context)
    {
        var result = new Dictionary<string, HashSet<string>>();

        // Get all syntax trees in the compilation
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            // Find all class declarations
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                if (classSymbol is null)
                {
                    continue;
                }

                // UNIFIED LOOKUP: Single check for all component types
                var componentBaseName = GetObservableComponentBaseName(classSymbol, context);

                if (componentBaseName is null)
                {
                    continue;
                }

                // This is a code-behind class - analyze Model property usages
                var usedProperties = AnalyzeModelPropertyUsages(classDecl, semanticModel);

                if (usedProperties.Count > 0)
                {
                    if (!result.ContainsKey(componentBaseName))
                    {
                        result[componentBaseName] = new HashSet<string>();
                    }

                    foreach (var property in usedProperties)
                    {
                        result[componentBaseName].Add(property);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a class inherits from an Observable component.
    /// Returns the component base class name if true, null otherwise.
    /// SIMPLIFIED: Uses unified GeneratorContext lookup instead of two parallel dictionaries.
    /// </summary>
    private static string? GetObservableComponentBaseName(
        INamedTypeSymbol classSymbol,
        GeneratorContext context)
    {
        var baseType = classSymbol.BaseType;
        while (baseType is not null)
        {
            var baseName = baseType.Name;

            // UNIFIED CHECK: Single lookup for both same-assembly and cross-assembly components
            if (context.AllComponents.ContainsKey(baseName))
            {
                return baseName;
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Analyzes a class declaration for all Model.PropertyName usages.
    /// Returns set of property chains (e.g., "IsDay", "Settings.Theme").
    /// </summary>
    private static HashSet<string> AnalyzeModelPropertyUsages(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel)
    {
        var usedProperties = new HashSet<string>();

        // Find all member access expressions (e.g., Model.IsDay, Model.Settings.Theme)
        var memberAccesses = classDecl.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>();

        foreach (var memberAccess in memberAccesses)
        {
            var propertyChain = ExtractModelPropertyChain(memberAccess, semanticModel);
            if (propertyChain is not null && propertyChain.Length > 0)
            {
                usedProperties.Add(propertyChain);
            }
        }

        return usedProperties;
    }

    /// <summary>
    /// Extracts Model property chain from a member access expression.
    /// Simply extracts the full chain without validation - SSOT dictionary will validate.
    /// E.g., "Model.CurrentUser.Identity.IsAuthenticated" → "CurrentUser.Identity.IsAuthenticated"
    /// E.g., "Model.Settings.Theme" → "Settings.Theme"
    /// Returns null if not a Model property access.
    /// </summary>
    private static string? ExtractModelPropertyChain(
        MemberAccessExpressionSyntax memberAccess,
        SemanticModel semanticModel)
    {
        // Build the full chain by walking up the expression tree
        var chain = new List<string>();
        var current = memberAccess;

        while (current is not null)
        {
            // Add the member name to the chain (e.g., "IsAuthenticated", "Identity", "CurrentUser")
            chain.Insert(0, current.Name.Identifier.Text);

            // Check if the expression is "Model"
            if (current.Expression is IdentifierNameSyntax identifier)
            {
                if (identifier.Identifier.Text == "Model")
                {
                    // This is a Model property access - return the full chain
                    // The SSOT dictionary will validate which parts are actually observable
                    var propertyChain = string.Join(".", chain);
                    return StripCommandMembers(propertyChain);
                }

                // Not a Model access - stop
                break;
            }

            // Continue walking up the chain
            if (current.Expression is MemberAccessExpressionSyntax parentAccess)
            {
                current = parentAccess;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Strips command interface members from property chains.
    /// E.g., "RefreshCommand.Executing" → "RefreshCommand"
    /// E.g., "LoadWeatherCommand.ExecuteAsync" → "LoadWeatherCommand"
    /// </summary>
    private static readonly string[] CommandMembers = new[]
    {
        "CanExecute",
        "Error",
        "ResetError",
        "Executing",
        "Cancel",
        "Execute",
        "ExecuteAsync"
    };

    private static string StripCommandMembers(string propertyChain)
    {
        var parts = propertyChain.Split('.');

        // Remove command members from the end
        while (parts.Length > 1 && CommandMembers.Contains(parts[parts.Length - 1], StringComparer.Ordinal))
        {
            var newParts = new string[parts.Length - 1];
            Array.Copy(parts, newParts, newParts.Length);
            parts = newParts;
        }

        return parts.Length > 0 ? string.Join(".", parts) : string.Empty;
    }
}
