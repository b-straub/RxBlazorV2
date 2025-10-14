using Microsoft.CodeAnalysis;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analysis;

/// <summary>
/// Utility class for parsing .razor file content and extracting information.
/// Focuses on text parsing without semantic analysis.
/// </summary>
public static class RazorContentParser
{
    /// <summary>
    /// Extracts namespace from a file path.
    /// Example: /Users/.../RxBlazorV2Sample/Samples/CommandTriggers/Page.razor
    /// -> RxBlazorV2Sample.Samples.CommandTriggers
    /// </summary>
    public static string ExtractNamespaceFromPath(string filePath)
    {
        var segments = filePath.Replace('\\', '/').Split('/');
        var relevantSegments = new List<string>();
        var foundProject = false;

        foreach (var segment in segments)
        {
            if (segment.EndsWith(".csproj") || segment.EndsWith("Sample") || segment.EndsWith("Tests"))
            {
                relevantSegments.Clear();
                relevantSegments.Add(segment.Replace(".csproj", ""));
                foundProject = true;
                continue;
            }

            if (foundProject && !segment.EndsWith(".razor") && !segment.EndsWith(".cs"))
            {
                relevantSegments.Add(segment);
            }
        }

        return relevantSegments.Any() ? string.Join(".", relevantSegments) : "Unknown";
    }

    /// <summary>
    /// Parses razor content to find @inherits directive with ObservableComponent.
    /// Returns the model type if found, null otherwise.
    /// Handles generic types with multiple type parameters.
    /// </summary>
    public static string? ParseInheritsObservableComponent(string razorContent)
    {
        var inheritsMatch = Regex.Match(razorContent, @"@inherits\s+\S*ObservableComponent<(.+?)>\s*$", RegexOptions.Multiline);
        if (inheritsMatch.Success)
        {
            return inheritsMatch.Groups[1].Value;
        }
        return null;
    }

    /// <summary>
    /// Checks if razor content has @inherits ObservableComponent directive.
    /// </summary>
    public static bool HasObservableComponentInheritance(string razorContent)
    {
        return Regex.IsMatch(razorContent, @"@inherits\s+\S*ObservableComponent\s*$", RegexOptions.Multiline) ||
               Regex.IsMatch(razorContent, @"@inherits\s+\S*ObservableComponent<.+?>\s*$", RegexOptions.Multiline);
    }

    /// <summary>
    /// Parses @inject directives from razor content.
    /// Returns a list of (typeName, fieldName) pairs.
    /// </summary>
    public static List<(string typeName, string fieldName)> ParseInjectDirectives(string razorContent)
    {
        var result = new List<(string, string)>();
        var injectMatches = Regex.Matches(razorContent, @"@inject\s+([a-zA-Z_][a-zA-Z0-9_\.]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)");

        foreach (Match injectMatch in injectMatches)
        {
            var typeName = injectMatch.Groups[1].Value;
            var fieldName = injectMatch.Groups[2].Value;
            result.Add((typeName, fieldName));
        }

        return result;
    }

    /// <summary>
    /// Gets simple type name from a potentially qualified type name.
    /// Example: "Namespace.Model" -> "Model"
    /// </summary>
    public static string GetSimpleTypeName(string typeName)
    {
        return typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;
    }

    /// <summary>
    /// Builds a map of simple type names to full type names from observable models.
    /// </summary>
    public static Dictionary<string, string> BuildTypeNameMap(ImmutableArray<ObservableModelInfo?> observableModels)
    {
        var typeNameToFullName = new Dictionary<string, string>();

        foreach (var model in observableModels.Where(m => m != null))
        {
            var simpleTypeName = model!.ClassName;
            var fullTypeName = model.FullyQualifiedName;
            typeNameToFullName[simpleTypeName] = fullTypeName;
        }

        return typeNameToFullName;
    }
}
