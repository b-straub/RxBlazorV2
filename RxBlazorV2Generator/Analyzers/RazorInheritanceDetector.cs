using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analyzers;

/// <summary>
/// Lightweight scanner that detects direct @inherits ObservableComponent usage in razor files.
/// Reports an error instructing users to use [ObservableComponent] attribute instead.
/// </summary>
public static class RazorInheritanceDetector
{
    private static readonly Regex InheritsPattern = new(
        @"@inherits\s+ObservableComponent(?:<[^>]+>)?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Analyzes a razor file for direct ObservableComponent inheritance.
    /// Returns component name and generic type info if found.
    /// </summary>
    public static (string componentName, string genericPart)? DetectDirectInheritance(
        AdditionalText razorFile,
        SourceText content)
    {
        try
        {
            var text = content.ToString();
            var match = InheritsPattern.Match(text);

            if (!match.Success)
            {
                return null;
            }

            // Extract component name from file path (e.g., Pages/Weather.razor -> Weather)
            var fileName = System.IO.Path.GetFileNameWithoutExtension(razorFile.Path);

            // Extract generic part (e.g., "<WeatherModel>" or empty string)
            var inheritStatement = match.Value;
            var genericPart = inheritStatement.Contains('<')
                ? inheritStatement.Substring(inheritStatement.IndexOf('<'))
                : string.Empty;

            return (fileName, genericPart);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a location for the razor file diagnostic.
    /// </summary>
    public static Location CreateRazorFileLocation(AdditionalText razorFile, SourceText content)
    {
        try
        {
            var text = content.ToString();
            var match = InheritsPattern.Match(text);

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
