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
        foreach (var singleton in singletons)
        {
            sb.AppendLine($"    /// <summary>Reference to {singleton.ClassName}</summary>");
            sb.AppendLine($"    public {singleton.FullyQualifiedName} {singleton.PropertyName} {{ get; }}");
        }

        sb.AppendLine();

        // Generate constructor
        GenerateConstructor(sb, singletons);

        // Generate OnContextReadyIntern
        GenerateOnContextReadyIntern(sb, singletons);

        // Generate OnContextReadyInternAsync
        GenerateOnContextReadyInternAsync(sb, singletons);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateConstructor(StringBuilder sb, List<SingletonModelInfo> singletons)
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

    private static void GenerateOnContextReadyIntern(StringBuilder sb, List<SingletonModelInfo> singletons)
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

    private static void GenerateOnContextReadyInternAsync(StringBuilder sb, List<SingletonModelInfo> singletons)
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
}
