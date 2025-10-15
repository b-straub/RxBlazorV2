using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analyzers;

/// <summary>
/// Scans razor files to detect usage of generated component classes.
/// Used to count how many razor files inherit from a specific component (e.g., @inherits WeatherModelComponent).
/// </summary>
public static class RazorComponentUsageDetector
{
    /// <summary>
    /// Detects if a razor file inherits from a specific component class.
    /// Returns the location of the @inherits statement if found.
    /// </summary>
    public static Location? DetectComponentUsage(
        AdditionalText razorFile,
        SourceText content,
        string componentClassName)
    {
        try
        {
            var text = content.ToString();

            // Create pattern to match @inherits {ComponentClassName}
            // This matches the component name as a whole word (not part of another word)
            var pattern = new Regex(
                $@"@inherits\s+{Regex.Escape(componentClassName)}\b",
                RegexOptions.Compiled | RegexOptions.Multiline);

            var match = pattern.Match(text);

            if (!match.Success)
            {
                return null;
            }

            // Create location for the match
            var lineNumber = content.Lines.GetLinePosition(match.Index).Line;
            var lineSpan = content.Lines[lineNumber].Span;
            var textSpan = new TextSpan(lineSpan.Start, lineSpan.Length);
            return Location.Create(razorFile.Path, textSpan, content.Lines.GetLinePositionSpan(textSpan));
        }
        catch
        {
            return null;
        }
    }
}
