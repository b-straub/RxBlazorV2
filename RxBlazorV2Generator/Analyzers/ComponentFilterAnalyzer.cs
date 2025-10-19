using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analyzers;

/// <summary>
/// Analyzes razor files for component inheritance and property usage.
/// </summary>
public static class ComponentFilterAnalyzer
{
    private static readonly Regex InheritsPattern = new(
        @"@inherits\s+([^\r\n]+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Extracts the @inherits directive from a razor file.
    /// Returns the inherited type name or null if not found.
    /// </summary>
    public static string? ExtractInheritsType(string razorContent)
    {
        try
        {
            var inheritsMatch = InheritsPattern.Match(razorContent);
            return inheritsMatch.Success ? inheritsMatch.Groups[1].Value.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Common method names that should be filtered out from property chains.
    /// These are method calls, not property accesses, and shouldn't be included in the filter.
    /// </summary>
    private static readonly string[] CommonMethodNames = new[]
    {
        // LINQ methods (excluding Count which is also a common property)
        "Any", "All", "First", "FirstOrDefault", "Last", "LastOrDefault",
        "Single", "SingleOrDefault", "Sum", "Min", "Max", "Average",
        "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending",
        "ThenBy", "ThenByDescending", "Take", "Skip", "Distinct",
        "ToArray", "ToList", "ToDictionary", "GroupBy", "Join", "Aggregate",

        // Common object methods
        "ToString", "GetHashCode", "Equals", "GetType",

        // String methods
        "Substring", "Replace", "Trim", "TrimStart", "TrimEnd", "ToUpper", "ToLower",
        "StartsWith", "EndsWith", "IndexOf", "Split", "Join", "Format",

        // Collection methods (excluding Count and Contains which are also properties)
        "Add", "Remove", "Clear", "Insert", "RemoveAt",
        "CopyTo", "GetEnumerator"
    };

    /// <summary>
    /// Analyzes razor content for Model.PropertyName patterns using simple regex.
    /// Returns list of ALL property chains accessed via Model (e.g., "IsDay", "Settings.Theme").
    /// Uses C# identifier rules: valid identifier starts with letter/underscore, followed by letters/digits/underscores.
    /// Note: Doesn't validate if properties exist - over-inclusion is harmless for filtering.
    /// This approach works across assembly boundaries where we can't resolve type information.
    /// Strips common method calls from property chains (e.g., "Forecasts.Any" → "Forecasts").
    /// </summary>
    public static List<string> AnalyzePropertyUsage(
        string razorContent,
        string modelFieldName)
    {
        var usedMembers = new HashSet<string>();

        // Match exactly "Model." followed by valid C# property chain
        // Pattern: Model.Identifier or Model.Identifier.Identifier.Identifier...
        // Valid identifier: starts with letter or underscore, then letters, digits, underscores
        // Examples: Model.IsDay → "IsDay", Model.Settings.Theme → "Settings.Theme"
        var propertyPattern = @"\bModel\.([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)";
        var matches = Regex.Matches(razorContent, propertyPattern);

        foreach (Match match in matches)
        {
            var memberChain = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(memberChain))
            {
                // Strip common method calls from the end of the chain
                // E.g., "Forecasts.Any" → "Forecasts", "LastRefresh.ToString" → "LastRefresh"
                var cleaned = StripMethodCalls(memberChain);
                if (!string.IsNullOrEmpty(cleaned))
                {
                    usedMembers.Add(cleaned);
                }
            }
        }

        return usedMembers.ToList();
    }

    /// <summary>
    /// Strips known method calls from the end of a property chain.
    /// E.g., "Forecasts.Any" → "Forecasts", "Settings.Theme.ToString" → "Settings.Theme"
    /// </summary>
    private static string StripMethodCalls(string propertyChain)
    {
        var parts = propertyChain.Split('.');

        // Work backwards, removing known method names from the end
        while (parts.Length > 0 && CommonMethodNames.Contains(parts[parts.Length - 1], StringComparer.Ordinal))
        {
            // Remove last element (.NET Standard 2.0 compatible)
            var newParts = new string[parts.Length - 1];
            Array.Copy(parts, newParts, newParts.Length);
            parts = newParts;
        }

        return parts.Length > 0 ? string.Join(".", parts) : string.Empty;
    }

    /// <summary>
    /// Creates a location for the razor file diagnostic.
    /// </summary>
    public static Location CreateRazorFileLocation(AdditionalText razorFile, SourceText content, string pattern)
    {
        try
        {
            var text = content.ToString();
            var regex = new Regex(pattern, RegexOptions.Compiled);
            var match = regex.Match(text);

            if (match.Success)
            {
                var lineNumber = content.Lines.GetLinePosition(match.Index).Line;
                var lineSpan = content.Lines[lineNumber].Span;
                var textSpan = new TextSpan(lineSpan.Start, lineSpan.Length);
                return Location.Create(razorFile.Path, textSpan, content.Lines.GetLinePositionSpan(textSpan));
            }

            // Fallback to start of file
            return Location.Create(razorFile.Path, default, default);
        }
        catch
        {
            return Location.Create(razorFile.Path, default, default);
        }
    }

}
