using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates the RxBlazorV2 layout model that ensures all singleton models are instantiated together.
/// </summary>
public static class SingletonAggregatorTemplate
{
    /// <summary>
    /// Generates the RxBlazorV2LayoutModel class.
    /// </summary>
    /// <param name="singletons">List of singleton models to aggregate</param>
    /// <param name="rootNamespace">The root namespace for the generated class</param>
    /// <returns>Generated C# code for the layout model</returns>
    public static string GenerateAggregationModel(List<SingletonModelInfo> singletons, string rootNamespace)
    {
        // Disambiguate duplicate property names before generating code
        var singletonsWithUniqueNames = DisambiguatePropertyNames(singletons);

        var sb = new StringBuilder();

        // Header - use fully qualified names for singleton types to avoid ambiguity
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using R3;");
        sb.AppendLine("using RxBlazorV2.Model;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Layout;");
        sb.AppendLine();

        // Class declaration
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated layout model for RxBlazorV2.");
        sb.AppendLine("/// Ensures all singleton ObservableModels are instantiated together,");
        sb.AppendLine("/// preventing initialization order issues with cross-model observers.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public partial class RxBlazorV2LayoutModel : ObservableModel");
        sb.AppendLine("{");

        // ModelID property
        sb.AppendLine($"    public override string ModelID => \"{rootNamespace}.Layout.RxBlazorV2LayoutModel\";");
        sb.AppendLine();

        // FilterUsedProperties - pass through all (no filtering)
        sb.AppendLine("    public override bool FilterUsedProperties(params string[] propertyNames)");
        sb.AppendLine("    {");
        sb.AppendLine("        return propertyNames.Length > 0;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate properties for each singleton (public for access from razor components)
        // Use fully qualified names to avoid ambiguity when multiple namespaces have same type names
        foreach (var singleton in singletonsWithUniqueNames)
        {
            sb.AppendLine($"    /// <summary>Reference to {singleton.ClassName}</summary>");
            sb.AppendLine($"    public {singleton.FullyQualifiedName} {singleton.PropertyName} {{ get; }}");
        }

        sb.AppendLine();

        // Generate constructor
        GenerateConstructor(sb, singletonsWithUniqueNames);

        // Generate OnContextReadyIntern
        GenerateOnContextReadyIntern(sb, singletonsWithUniqueNames);

        // Generate OnContextReadyInternAsync
        GenerateOnContextReadyInternAsync(sb, singletonsWithUniqueNames);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateConstructor(StringBuilder sb, List<DisambiguatedSingletonInfo> singletons)
    {
        // Constructor signature - use fully qualified names for parameter types
        sb.Append("    public RxBlazorV2LayoutModel(");

        var parameters = singletons.Select(s => $"{s.FullyQualifiedName} {s.ParameterName}");
        sb.Append(string.Join(", ", parameters));

        sb.AppendLine(") : base()");
        sb.AppendLine("    {");

        // Property assignments
        foreach (var singleton in singletons)
        {
            sb.AppendLine($"        {singleton.PropertyName} = {singleton.ParameterName};");
        }

        sb.AppendLine();

        // Observable subscriptions - transform property names
        foreach (var singleton in singletons)
        {
            sb.AppendLine($"        // Subscribe to {singleton.ClassName} changes");
            sb.AppendLine($"        Subscriptions.Add({singleton.PropertyName}.Observable");
            sb.AppendLine($"            .Select(props => props.Select(p => p.Replace(\"Model.\", \"Model.{singleton.PropertyName}.\")).ToArray())");
            sb.AppendLine($"            .Subscribe(props => StateHasChanged(props)));");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateOnContextReadyIntern(StringBuilder sb, List<DisambiguatedSingletonInfo> singletons)
    {
        sb.AppendLine("    private bool _contextReadyInternCalled;");
        sb.AppendLine();
        sb.AppendLine("    protected override void OnContextReadyIntern()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_contextReadyInternCalled)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        _contextReadyInternCalled = true;");
        sb.AppendLine();

        foreach (var singleton in singletons)
        {
            sb.AppendLine($"        {singleton.PropertyName}.ContextReady();");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateOnContextReadyInternAsync(StringBuilder sb, List<DisambiguatedSingletonInfo> singletons)
    {
        sb.AppendLine("    private bool _contextReadyInternAsyncCalled;");
        sb.AppendLine();
        sb.AppendLine("    protected override async Task OnContextReadyInternAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_contextReadyInternAsyncCalled)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        _contextReadyInternAsyncCalled = true;");
        sb.AppendLine();

        foreach (var singleton in singletons)
        {
            sb.AppendLine($"        await {singleton.PropertyName}.ContextReadyAsync();");
        }

        sb.AppendLine("    }");
    }

    /// <summary>
    /// Detects duplicate property names and disambiguates them by appending namespace parts.
    /// E.g., two "StatusModel" classes from different namespaces get unique property names:
    /// - ReactivePatternSample.Status.Models.StatusModel → "ModelsStatus"
    /// - RxBlazorV2.MudBlazor.Components.StatusModel → "ComponentsStatus"
    /// </summary>
    private static List<DisambiguatedSingletonInfo> DisambiguatePropertyNames(List<SingletonModelInfo> singletons)
    {
        // Group by property name to find duplicates
        var duplicateGroups = singletons
            .GroupBy(s => s.PropertyName)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<DisambiguatedSingletonInfo>();

        foreach (var singleton in singletons)
        {
            if (duplicateGroups.TryGetValue(singleton.PropertyName, out var duplicates))
            {
                // This property name has duplicates - disambiguate using last namespace segment
                var lastNamespacePart = GetLastNamespacePart(singleton.Namespace);
                var uniquePropertyName = $"{lastNamespacePart}{singleton.PropertyName}";
                var uniqueParameterName = char.ToLowerInvariant(uniquePropertyName[0]) + uniquePropertyName.Substring(1);

                result.Add(new DisambiguatedSingletonInfo(
                    singleton.FullyQualifiedName,
                    singleton.ClassName,
                    singleton.Namespace,
                    uniquePropertyName,
                    uniqueParameterName));
            }
            else
            {
                // No duplicates - use original names
                result.Add(new DisambiguatedSingletonInfo(
                    singleton.FullyQualifiedName,
                    singleton.ClassName,
                    singleton.Namespace,
                    singleton.PropertyName,
                    singleton.ParameterName));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the last segment of a namespace (e.g., "Models" from "ReactivePatternSample.Status.Models")
    /// </summary>
    private static string GetLastNamespacePart(string namespaceName)
    {
        var parts = namespaceName.Split('.');
        return parts.Length > 0 ? parts[parts.Length - 1] : namespaceName;
    }
}

/// <summary>
/// Holds singleton info with disambiguated property/parameter names.
/// </summary>
internal record DisambiguatedSingletonInfo(
    string FullyQualifiedName,
    string ClassName,
    string Namespace,
    string PropertyName,
    string ParameterName);
