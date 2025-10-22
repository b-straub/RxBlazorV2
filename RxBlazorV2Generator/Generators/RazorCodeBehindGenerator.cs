using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Models;
using System.Text;
using System.Collections.Immutable;

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
    /// Uses simple regex-based property detection that works across assembly boundaries.
    /// </summary>
    public static void GenerateComponentFilterCodeBehind(
        SourceProductionContext context,
        AdditionalText razorFile,
        SourceText razorContent,
        Dictionary<string, string> componentNamespaces,
        Dictionary<string, (string Namespace, INamedTypeSymbol TypeSymbol)> crossAssemblyComponents)
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

            // Check if this is an Observable component
            // Strategy 1: Check same-assembly components (from observableModelRecords)
            var isSameAssemblyComponent = componentNamespaces.ContainsKey(componentTypeName);

            // Strategy 2: Check cross-assembly components (accurate inheritance detection)
            var isCrossAssemblyComponent = false;
            if (!isSameAssemblyComponent)
            {
                // Extract namespace from inheritsType (e.g., "MyNamespace.ComponentName" -> "MyNamespace")
                var inheritsNamespace = ExtractNamespace(inheritsType);

                // Check if component exists in cross-assembly components map
                if (crossAssemblyComponents.TryGetValue(componentTypeName, out var componentInfo))
                {
                    // If namespace is specified in @inherits, verify it matches
                    if (!string.IsNullOrEmpty(inheritsNamespace))
                    {
                        if (componentInfo.Namespace == inheritsNamespace)
                        {
                            isCrossAssemblyComponent = true;
                        }
                    }
                    else
                    {
                        // No namespace specified in @inherits, accept the match
                        isCrossAssemblyComponent = true;
                    }
                }
            }

            // If not an Observable component, skip code generation
            if (!isSameAssemblyComponent && !isCrossAssemblyComponent)
            {
                return;
            }

            // Analyze property usage in razor file using simple regex
            // Note: We use "Model" as the field name since that's what ObservableComponent<T> uses
            var usedProperties = ComponentFilterAnalyzer.AnalyzePropertyUsage(
                razorContentText,
                "Model");

            // Generate code-behind file
            var sb = new StringBuilder();
            var componentName = Path.GetFileNameWithoutExtension(razorFile.Path);

            // Extract namespace from razor file (check @namespace directive first, then fall back to path)
            var namespaceName = ExtractNamespaceFromRazorFile(razorContentText, razorFile.Path);

            // Extract using directives from razor file
            var usingDirectives = ExtractUsingDirectives(razorContentText);

            // Add using directive for component type's namespace if needed
            if (componentNamespaces.TryGetValue(componentTypeName, out var componentNamespace))
            {
                if (componentNamespace != namespaceName && !usingDirectives.Contains(componentNamespace))
                {
                    usingDirectives.Add(componentNamespace);
                }
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
                // Strip command interface members and prepend "Model."
                // E.g., captured "ToggleThemeCommand.Execute" → "Model.ToggleThemeCommand"
                // E.g., captured "RefreshCommand.Executing" → "Model.RefreshCommand"
                // E.g., captured "Settings.IsDay" → "Model.Settings.IsDay"
                // Observable streams emit "Model.PropertyName" for consistency
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
                        return $"Model.{cleaned}";
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
                // No properties detected - use empty filter (observe all changes)
                sb.AppendLine("        // No properties detected in razor file - observe all changes");
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
    private static string ExtractNamespaceFromRazorFile(string razorContent, string filePath)
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
        return ExtractNamespaceFromPath(filePath);
    }

    /// <summary>
    /// Extracts namespace from razor file path.
    /// Example: /Project/Components/Settings.razor -> Project.Components
    /// </summary>
    private static string ExtractNamespaceFromPath(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
            {
                return "Global";
            }

            // Find the project root by looking for common project markers
            // For now, we'll take the last two directory segments
            var segments = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Filter out empty segments
            segments = segments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            if (segments.Length >= 2)
            {
                // Take last two segments (e.g., "RxBlazorV2Sample" + "Components")
                var lastTwo = segments.Skip(segments.Length - 2).ToArray();
                return string.Join(".", lastTwo);
            }
            else if (segments.Length == 1)
            {
                return segments[0];
            }

            return "Global";
        }
        catch
        {
            return "Global";
        }
    }

}
