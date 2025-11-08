using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Models;
using System.Text;
using System.Collections.Immutable;
using RxBlazorV2Generator.Builders;

namespace RxBlazorV2Generator.Generators;

/// <summary>
/// Generates code-behind (.razor.cs) files for razor components that inherit from ObservableComponents.
/// Analyzes property usage in razor files and generates Filter() method for optimal re-rendering.
/// </summary>
public static class RazorCodeBehindGenerator
{
    /// <summary>
    /// Members from IObservableCommand interfaces that should be stripped from property chains.
    /// These are methods/properties on the command itself, not the model property.
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

    /// <summary>
    /// Generates .razor.cs code-behind file with Filter() method for components that inherit from ObservableComponents.
    /// NEW UNIFIED APPROACH: Uses GeneratorContext as single source of truth for all component metadata.
    /// Fixes cross-assembly bugs: missing using directives and missing "Model." prefix.
    /// </summary>
    public static void GenerateComponentFilterCodeBehind(
        SourceProductionContext context,
        AdditionalText razorFile,
        SourceText razorContent,
        GeneratorContext generatorContext,
        Dictionary<string, HashSet<string>> codeBehindPropertyUsages,
        GeneratorConfig config)
    {
        try
        {
            // Extract @inherits directive
            var razorContentText = razorContent.ToString();
            var inheritsType = ComponentFilterAnalyzer.ExtractInheritsType(razorContentText);

            if (inheritsType is null)
            {
                return; // No @inherits directive, nothing to generate
            }

            // Extract component type name (without generic parameters)
            var componentTypeName = ExtractComponentTypeName(inheritsType);

            // UNIFIED LOOKUP: Single check for both same-assembly and cross-assembly components
            if (!generatorContext.AllComponents.TryGetValue(componentTypeName, out var component))
            {
                return; // Not an observable component
            }

            // Analyze property usage in razor file using simple regex (no validation)
            var razorUsedProperties = ComponentFilterAnalyzer.AnalyzePropertyUsage(
                razorContentText,
                "Model");

            // Merge with code-behind property usages from .razor.cs files
            var extractedProperties = new HashSet<string>();
            foreach (var prop in razorUsedProperties)
            {
                extractedProperties.Add(prop);
            }

            if (codeBehindPropertyUsages.TryGetValue(componentTypeName, out var codeBehindProps))
            {
                foreach (var prop in codeBehindProps)
                {
                    extractedProperties.Add(prop);
                }
            }

            // Validate against SSOT filterable properties (already have "Model." prefix)
            var usedProperties = new HashSet<string>();
            foreach (var extractedProp in extractedProperties)
            {
                var matchedProperty = FindMatchingValidProperty(extractedProp, component.FilterableProperties);
                if (matchedProperty is not null)
                {
                    usedProperties.Add(matchedProperty);
                }
            }

            // Add component trigger properties to filter for automatic re-rendering
            // UNLESS they are HookOnly (TriggerBehavior == 2)
            // HookOnly triggers execute hooks but don't trigger component re-renders
            if (component.ComponentInfo is not null)
            {
                foreach (var trigger in component.ComponentInfo.ComponentTriggers)
                {
                    // Only add to filter if NOT HookOnly (2 = HookOnly)
                    if (trigger.TriggerBehavior != 2)
                    {
                        usedProperties.Add(trigger.QualifiedPropertyPath);
                    }
                }
            }

            // Report diagnostic if component has no reactive properties and no triggers
            if (usedProperties.Count == 0 && !component.HasTriggers)
            {
                var componentFileName = Path.GetFileNameWithoutExtension(razorFile.Path);

                // Create a location for the razor file to help users identify the issue
                var location = Location.Create(
                    razorFile.Path,
                    textSpan: default,
                    lineSpan: new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                        new Microsoft.CodeAnalysis.Text.LinePosition(0, 0),
                        new Microsoft.CodeAnalysis.Text.LinePosition(0, 0)));

                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.NonReactiveComponentError,
                    location,
                    componentFileName);
                context.ReportDiagnostic(diagnostic);
                return; // Don't generate - component serves no reactive purpose
            }

            // Generate code-behind file
            var sb = new StringBuilder();
            var componentName = Path.GetFileNameWithoutExtension(razorFile.Path);

            // Extract namespace from razor file location
            // NOTE: component.Namespace is the BASE CLASS namespace, not the razor file namespace
            var namespaceName = ExtractNamespaceFromRazorFile(razorContentText, razorFile.Path, config.RootNamespace);

            // Extract using directives from razor file
            var usingDirectives = ExtractUsingDirectives(razorContentText);

            // FIX #1: Add using directive for base component namespace if cross-assembly
            if (component.BaseComponentNamespace is not null && component.BaseComponentNamespace.Length > 0)
            {
                if (component.BaseComponentNamespace != namespaceName &&
                    !usingDirectives.Contains(component.BaseComponentNamespace))
                {
                    usingDirectives.Add(component.BaseComponentNamespace);
                }
            }

            // Add using directive for component's own namespace if needed
            if (component.Namespace != namespaceName && !usingDirectives.Contains(component.Namespace))
            {
                usingDirectives.Add(component.Namespace);
            }

            // Add using directives
            foreach (var usingDirective in usingDirectives.OrderBy(u => u))
            {
                sb.AppendLine($"using {usingDirective};");
            }
            if (usingDirectives.Any())
            {
                sb.AppendLine();
            }

            // Generate code-behind with base class from @inherits
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
            sb.AppendLine($"public partial class {componentName} : {inheritsType}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated filter method for ObservableComponent property filtering.");
            sb.AppendLine("    /// Only properties used in this component trigger re-renders.");
            sb.AppendLine("    /// Property names match the qualified names emitted by Observable streams (ClassName.PropertyName).");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    protected override string[] Filter()");
            sb.AppendLine("    {");

            if (usedProperties.Count > 0)
            {
                // FIX #2: Properties already have "Model." prefix from GeneratorContext
                var filterProperties = usedProperties
                    .Select(prop =>
                    {
                        // Strip any IObservableCommand interface members
                        var cleaned = prop;
                        foreach (var member in CommandMembers)
                        {
                            var suffix = $".{member}";
                            if (cleaned.EndsWith(suffix, StringComparison.Ordinal))
                            {
                                cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length);
                                break;
                            }
                        }
                        return cleaned;
                    })
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                sb.AppendLine("        return [");
                for (var i = 0; i < filterProperties.Count; i++)
                {
                    var comma = i < filterProperties.Count - 1 ? "," : "";
                    sb.AppendLine($"            \"{filterProperties[i]}\"{comma}");
                }
                sb.AppendLine("        ];");
            }
            else
            {
                // No properties detected - use empty filter (no automatic StateHasChanged, only triggers)
                sb.AppendLine("        // No properties detected in razor file - no automatic StateHasChanged");
                sb.AppendLine("        return [];");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Add source to compilation
            var fileName = $"{componentName}.g.cs";
            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            // Report code generation error
            var diagnostic = Diagnostic.Create(
                Diagnostics.DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                Path.GetFileName(razorFile.Path),
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Extracts using directives from razor file.
    /// </summary>
    private static List<string> ExtractUsingDirectives(string razorContent)
    {
        var usings = new List<string>();
        var usingMatches = System.Text.RegularExpressions.Regex.Matches(
            razorContent,
            @"@using\s+([a-zA-Z_][a-zA-Z0-9_.]*)",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        foreach (System.Text.RegularExpressions.Match match in usingMatches)
        {
            if (match.Success)
            {
                usings.Add(match.Groups[1].Value);
            }
        }

        return usings;
    }

    /// <summary>
    /// Extracts the component type name without generic parameters or namespace.
    /// Example: "GenericModelsModelComponent&lt;string, int&gt;" → "GenericModelsModelComponent"
    /// Example: "MyNamespace.WeatherModelComponent" → "WeatherModelComponent"
    /// Example: "WeatherModelComponent" → "WeatherModelComponent"
    /// </summary>
    private static string ExtractComponentTypeName(string inheritsType)
    {
        // Remove namespace first
        var lastDot = inheritsType.LastIndexOf('.');
        var typeName = lastDot >= 0 ? inheritsType.Substring(lastDot + 1) : inheritsType;

        // Remove generic parameters
        var genericStart = typeName.IndexOf('<');
        if (genericStart > 0)
        {
            return typeName.Substring(0, genericStart);
        }
        return typeName;
    }

    /// <summary>
    /// Extracts namespace from a fully qualified type name.
    /// Example: "MyNamespace.WeatherModelComponent" → "MyNamespace"
    /// Example: "WeatherModelComponent" → ""
    /// </summary>
    private static string ExtractNamespace(string inheritsType)
    {
        // Remove generic parameters first
        var genericStart = inheritsType.IndexOf('<');
        var typeWithoutGenerics = genericStart > 0 ? inheritsType.Substring(0, genericStart) : inheritsType;

        // Extract namespace
        var lastDot = typeWithoutGenerics.LastIndexOf('.');
        return lastDot >= 0 ? typeWithoutGenerics.Substring(0, lastDot) : string.Empty;
    }

    /// <summary>
    /// Extracts namespace from razor file, checking @namespace directive first, then falling back to path.
    /// </summary>
    private static string ExtractNamespaceFromRazorFile(string razorContent, string filePath, string rootNamespace)
    {
        // First, try to find @namespace directive
        var namespaceMatch = System.Text.RegularExpressions.Regex.Match(
            razorContent,
            @"@namespace\s+([a-zA-Z_][a-zA-Z0-9_.]*)",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        if (namespaceMatch.Success)
        {
            return namespaceMatch.Groups[1].Value;
        }

        // Fall back to path-based extraction
        return ExtractNamespaceFromPath(filePath, rootNamespace);
    }

    /// <summary>
    /// Extracts namespace from razor file path using root namespace and relative path.
    /// Example:
    ///   Root: WebAppBase.Shared
    ///   Path: /Project/WebAppBase.Shared/Components/Push/PushManager.razor
    ///   Result: WebAppBase.Shared.Components.Push
    /// </summary>
    private static string ExtractNamespaceFromPath(string filePath, string rootNamespace)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return rootNamespace;
            }

            var directory = Path.GetDirectoryName(filePath);

            // For relative paths like "Components/MyWidget.razor", GetDirectoryName returns "Components"
            // For paths with no directory like "MyWidget.razor", it returns empty
            if (string.IsNullOrEmpty(directory))
            {
                // No directory in path - use root namespace
                return rootNamespace;
            }

            // Try to find project root by looking for .csproj file
            string? projectRoot = null;
            try
            {
                projectRoot = FindProjectRoot(directory);
            }
            catch
            {
                // FindProjectRoot can fail if directory doesn't exist (test scenarios)
                // Fall through to use simple path-based logic
            }

            if (projectRoot is null)
            {
                // Fallback: no project root (common in tests or relative paths)
                // Simple rule: namespace follows directory structure
                var pathSegments = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                if (pathSegments.Length >= 2)
                {
                    // Take last two segments (e.g., "Test.Components" from "/foo/bar/Test/Components")
                    return string.Join(".", pathSegments.Skip(pathSegments.Length - 2));
                }
                else if (pathSegments.Length == 1)
                {
                    // Single segment (e.g., "Components" from "Components/MyWidget.razor")
                    return pathSegments[0];
                }

                // No segments - use root namespace
                return rootNamespace;
            }

            // Calculate relative path from project root to file directory
            var relativePath = GetRelativePath(projectRoot, directory);

            // If file is in project root, use just the root namespace
            if (string.IsNullOrEmpty(relativePath))
            {
                return rootNamespace;
            }

            // Split relative path into segments and combine with root namespace
            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (segments.Length == 0)
            {
                return rootNamespace;
            }

            // Combine root namespace with path segments
            return $"{rootNamespace}.{string.Join(".", segments)}";
        }
        catch
        {
            return rootNamespace;
        }
    }

    /// <summary>
    /// Finds the project root directory by walking up the directory tree looking for .csproj file.
    /// </summary>
    private static string? FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            // Check if this directory contains a .csproj file
            if (directory.GetFiles("*.csproj").Any())
            {
                return directory.FullName;
            }

            // Move up to parent directory
            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Gets the relative path from one directory to another (polyfill for .NET Standard 2.0).
    /// </summary>
    private static string GetRelativePath(string fromPath, string toPath)
    {
        if (string.IsNullOrEmpty(fromPath))
        {
            throw new ArgumentNullException(nameof(fromPath));
        }
        if (string.IsNullOrEmpty(toPath))
        {
            throw new ArgumentNullException(nameof(toPath));
        }

        // Normalize paths
        var fromUri = new Uri(AppendDirectorySeparator(fromPath));
        var toUri = new Uri(AppendDirectorySeparator(toPath));

        // If paths are on different drives/roots, can't get relative path
        if (!fromUri.IsBaseOf(toUri))
        {
            return string.Empty;
        }

        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        // Convert forward slashes to platform-specific directory separator
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

        return relativePath;
    }

    /// <summary>
    /// Appends directory separator to path if it doesn't already end with one.
    /// </summary>
    private static string AppendDirectorySeparator(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            return path + Path.DirectorySeparatorChar;
        }
        return path;
    }

    /// <summary>
    /// Finds the matching valid property for an extracted property chain.
    /// Prefers longest (most specific) match to avoid collapsing nested properties.
    /// E.g., extracted "PushModel.IsSupported" prefers "Model.PushModel.IsSupported" over "Model.PushModel"
    /// E.g., extracted "CurrentUser.Identity.IsAuthenticated" matches valid "Model.CurrentUser" (longest available)
    /// Returns the valid property name prefixed with "Model.", or null if no match.
    /// </summary>
    private static string? FindMatchingValidProperty(string extractedProp, HashSet<string> validProperties)
    {
        // Strategy: Find the LONGEST matching prefix (most specific)
        // This prevents collapsing "PushModel.IsSupported" to just "PushModel"

        // First, try exact match (most specific)
        var fullPropertyName = $"Model.{extractedProp}";
        if (validProperties.Contains(fullPropertyName))
        {
            return fullPropertyName;
        }

        // Try progressively shorter prefixes, but keep track of the longest match
        // E.g., "CurrentUser.Identity.IsAuthenticated" → try each prefix length from longest to shortest
        var parts = extractedProp.Split('.');

        // Start from the longest prefix (length - 1) and work down
        for (int length = parts.Length - 1; length > 0; length--)
        {
            var prefix = string.Join(".", parts.Take(length));
            var prefixWithModel = $"Model.{prefix}";

            if (validProperties.Contains(prefixWithModel))
            {
                // Found a match - return immediately (this is the longest/most specific match)
                return prefixWithModel;
            }
        }

        return null;
    }

}
