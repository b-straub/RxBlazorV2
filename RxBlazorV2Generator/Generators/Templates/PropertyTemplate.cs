using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates partial property implementations with field keyword and change notifications.
/// </summary>
public static class PropertyTemplate
{
    /// <summary>
    /// Generates the ModelID property implementation.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified name of the model.</param>
    /// <returns>Generated property code.</returns>
    public static string GenerateModelIDProperty(string fullyQualifiedName)
    {
        return $"    public override string ModelID => \"{fullyQualifiedName}\";";
    }

    /// <summary>
    /// Generates the Subscriptions property implementation.
    /// </summary>
    /// <returns>Generated property code.</returns>
    public static string GenerateSubscriptionsProperty()
    {
        var sb = new StringBuilder();
        sb.AppendLine("    private readonly CompositeDisposable _subscriptions = new();");
        sb.Append("    protected override IDisposable Subscriptions => _subscriptions;");
        return sb.ToString();
    }

    /// <summary>
    /// Generates protected properties for referenced models.
    /// </summary>
    /// <param name="modelReferences">Collection of model references.</param>
    /// <returns>Generated properties code.</returns>
    public static string GenerateModelReferenceProperties(IEnumerable<ModelReferenceInfo> modelReferences)
    {
        var sb = new StringBuilder();
        foreach (var modelRef in modelReferences)
        {
            sb.AppendLine($"    protected {modelRef.ReferencedModelTypeName} {modelRef.PropertyName} {{ get; private set; }}");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates partial property implementations with field keyword.
    /// </summary>
    /// <param name="partialProperties">Collection of partial properties.</param>
    /// <returns>Generated properties code.</returns>
    public static string GeneratePartialProperties(IEnumerable<PartialPropertyInfo> partialProperties)
    {
        var sb = new StringBuilder();
        var propertiesList = partialProperties.ToList();
        for (int i = 0; i < propertiesList.Count; i++)
        {
            sb.AppendLine(GeneratePartialProperty(propertiesList[i]));
            if (i < propertiesList.Count - 1)
            {
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Generates a single partial property implementation.
    /// </summary>
    /// <param name="prop">The property information.</param>
    /// <returns>Generated property code.</returns>
    private static string GeneratePartialProperty(PartialPropertyInfo prop)
    {
        var sb = new StringBuilder();
        var batchIdsParam = "";
        if (prop.BatchIds is not null && prop.BatchIds.Length > 0)
        {
            var quotedBatchIds = string.Join(", ", prop.BatchIds.Select(id => $"\"{id}\""));
            batchIdsParam = $", {quotedBatchIds}";
        }

        sb.AppendLine($"    public partial {prop.Type} {prop.Name}");
        sb.AppendLine("    {");
        sb.AppendLine("        get => field;");
        sb.AppendLine("        set");
        sb.AppendLine("        {");

        if (prop.IsEquatable)
        {
            sb.AppendLine("            if (field != value)");
            sb.AppendLine("            {");
            sb.AppendLine("                field = value;");
            sb.AppendLine($"                StateHasChanged(nameof({prop.Name}){batchIdsParam});");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine("            field = value;");
            sb.AppendLine($"            StateHasChanged(nameof({prop.Name}){batchIdsParam});");
        }

        sb.AppendLine("        }");
        sb.Append("    }");
        return sb.ToString();
    }
}
