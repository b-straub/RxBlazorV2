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
            var fileName = Path.GetFileNameWithoutExtension(razorFile.Path);

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

    /// <summary>
    /// Detects if razor file inherits from a specific component class and checks for @page directive.
    /// Returns location and whether the file has a @page directive.
    /// Used to detect same-assembly component usage without @page (RXBG061).
    /// </summary>
    public static (Location location, bool hasPage)? DetectComponentInheritanceWithoutPage(
        AdditionalText razorFile,
        SourceText content,
        string componentClassName)
    {
        try
        {
            var text = content.ToString();

            // Create pattern to match @inherits {ComponentClassName}
            // This matches both:
            // - Unqualified: @inherits SettingsModelComponent
            // - Fully-qualified: @inherits MyNamespace.SettingsModelComponent
            // Pattern: @inherits followed by optional namespace parts, ending with the component class name
            var inheritsPattern = new Regex(
                $@"@inherits\s+(?:[\w\.]+\.)?{Regex.Escape(componentClassName)}\b",
                RegexOptions.Compiled | RegexOptions.Multiline);

            var match = inheritsPattern.Match(text);

            if (!match.Success)
            {
                return null;
            }

            // Check for @page directive
            var hasPage = HasPageDirective(text);

            // Create location for the @inherits statement
            var lineNumber = content.Lines.GetLinePosition(match.Index).Line;
            var lineSpan = content.Lines[lineNumber].Span;
            var textSpan = new TextSpan(lineSpan.Start, lineSpan.Length);
            var location = Location.Create(razorFile.Path, textSpan, content.Lines.GetLinePositionSpan(textSpan));

            return (location, hasPage);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the text contains a @page directive.
    /// </summary>
    private static bool HasPageDirective(string text)
    {
        var pagePattern = new Regex(@"@page\s+""[^""]*""", RegexOptions.Compiled);
        return pagePattern.IsMatch(text);
    }

    /// <summary>
    /// Detects the default layout component from RouteView definitions.
    /// Searches for DefaultLayout="@typeof(ComponentName)" pattern in razor files.
    /// Returns the component name if found, otherwise null.
    /// </summary>
    public static string? DetectDefaultLayoutComponent(IEnumerable<AdditionalText> razorFiles)
    {
        try
        {
            // Pattern matches: DefaultLayout="@typeof(ComponentName)"
            var defaultLayoutPattern = new Regex(
                @"DefaultLayout\s*=\s*""@typeof\(([^)]+)\)""",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (var razorFile in razorFiles)
            {
                var content = razorFile.GetText();
                if (content is null)
                {
                    continue;
                }

                var text = content.ToString();
                var match = defaultLayoutPattern.Match(text);

                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a component (by its razor file name without extension) is used/rendered
    /// in any other razor file in the same assembly.
    /// Returns true if the component is used as a child component (e.g., &lt;ErrorDisplay /&gt;).
    /// </summary>
    public static bool IsComponentUsedInAssembly(
        string componentRazorFileName,
        IEnumerable<AdditionalText> allRazorFiles,
        AdditionalText currentRazorFile)
    {
        try
        {
            // Pattern matches component usage: <ComponentName or <ComponentName> or <ComponentName ...
            // This covers: <ErrorDisplay />, <ErrorDisplay>, <ErrorDisplay Param="..." />
            var componentUsagePattern = new Regex(
                $@"<{Regex.Escape(componentRazorFileName)}(?:\s|>|/>)",
                RegexOptions.Compiled | RegexOptions.Multiline);

            foreach (var razorFile in allRazorFiles)
            {
                // Skip the current file - we're looking for usage in OTHER files
                if (razorFile.Path == currentRazorFile.Path)
                {
                    continue;
                }

                var content = razorFile.GetText();
                if (content is null)
                {
                    continue;
                }

                var text = content.ToString();
                if (componentUsagePattern.IsMatch(text))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // In case of any error, conservatively assume the component is used
            // to avoid hiding potential same-assembly composition issues
            return true;
        }
    }
}
