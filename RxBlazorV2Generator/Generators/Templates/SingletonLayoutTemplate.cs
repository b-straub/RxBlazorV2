using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates the RxBlazorV2LayoutBase component that can be used as a layout base.
/// </summary>
public static class SingletonLayoutTemplate
{
    /// <summary>
    /// Generates the RxBlazorV2LayoutBase class.
    /// </summary>
    /// <param name="singletons">List of singleton models (for generating trigger hooks)</param>
    /// <param name="rootNamespace">The root namespace for the generated class</param>
    /// <param name="updateFrequencyMs">The observable update frequency in milliseconds</param>
    /// <returns>Generated C# code for the component</returns>
    public static string GenerateComponent(
        List<SingletonModelInfo> singletons,
        string rootNamespace,
        int updateFrequencyMs = 100)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("using R3;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using RxBlazorV2.Component;");

        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Layout;");
        sb.AppendLine();

        // Class declaration
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated layout base for RxBlazorV2.");
        sb.AppendLine("/// Use this as a base class for your main layout to ensure all singletons are initialized.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public partial class RxBlazorV2LayoutBase : ObservableComponent<RxBlazorV2LayoutModel>");
        sb.AppendLine("{");

        // InitializeGeneratedCode
        GenerateInitializeGeneratedCode(sb, singletons, updateFrequencyMs);

        // InitializeGeneratedCodeAsync
        sb.AppendLine();
        sb.AppendLine("    protected override Task InitializeGeneratedCodeAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        return Task.CompletedTask;");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateInitializeGeneratedCode(
        StringBuilder sb,
        List<SingletonModelInfo> singletons,
        int updateFrequencyMs)
    {
        sb.AppendLine("    protected override void InitializeGeneratedCode()");
        sb.AppendLine("    {");

        // Subscribe to model changes with filtering
        sb.AppendLine("        // Subscribe to model changes - respects Filter() method");
        sb.AppendLine("        var filter = Filter();");
        sb.AppendLine("        if (filter.Length > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Filter active - observe only filtered properties");
        sb.AppendLine("            Subscriptions.Add(Model.Observable");
        sb.AppendLine("                .Where(changedProps => changedProps.Intersect(filter).Any())");
        sb.AppendLine($"                .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
        sb.AppendLine("                .Subscribe(chunks =>");
        sb.AppendLine("                {");
        sb.AppendLine("                    InvokeAsync(StateHasChanged);");
        sb.AppendLine("                }));");
        sb.AppendLine("        }");
        sb.AppendLine("        // else: Empty filter - no automatic StateHasChanged");

        sb.AppendLine("    }");
    }
}
