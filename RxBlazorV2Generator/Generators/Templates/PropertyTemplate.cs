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
    /// Generates public properties for referenced models.
    /// Changed from protected to public to allow component access.
    /// </summary>
    /// <param name="modelReferences">Collection of model references.</param>
    /// <returns>Generated properties code.</returns>
    public static string GenerateModelReferenceProperties(IEnumerable<ModelReferenceInfo> modelReferences)
    {
        var sb = new StringBuilder();
        foreach (var modelRef in modelReferences)
        {
            sb.AppendLine($"    public {modelRef.ReferencedModelTypeName} {modelRef.PropertyName} {{ get; }}");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates properties for DI injected services.
    /// Services with ObservableModelObserver methods are protected (like model references).
    /// Other services are public to allow component access.
    /// </summary>
    /// <param name="diFields">Collection of DI fields.</param>
    /// <returns>Generated properties code.</returns>
    public static string GenerateDIFieldProperties(IEnumerable<DIFieldInfo> diFields)
    {
        var sb = new StringBuilder();
        foreach (var diField in diFields)
        {
            // Services with model observers use protected visibility (like model references)
            var accessibility = diField.HasModelObservers ? "protected" : "public";
            sb.AppendLine($"    {accessibility} {diField.FieldType} {diField.FieldName} {{ get; }}");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates partial property implementations with field keyword.
    /// Uses "Model." prefix for property names in StateHasChanged calls (component context).
    /// </summary>
    /// <param name="partialProperties">Collection of partial properties.</param>
    /// <param name="className">The class name (unused, kept for compatibility).</param>
    /// <returns>Generated properties code.</returns>
    public static string GeneratePartialProperties(IEnumerable<PartialPropertyInfo> partialProperties, string className)
    {
        var sb = new StringBuilder();
        var propertiesList = partialProperties.ToList();
        for (var i = 0; i < propertiesList.Count; i++)
        {
            sb.AppendLine(GeneratePartialProperty(propertiesList[i], className));
            if (i < propertiesList.Count - 1)
            {
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Generates a single partial property implementation with Model. prefix.
    /// </summary>
    /// <param name="prop">The property information.</param>
    /// <param name="className">The class name (unused, kept for compatibility).</param>
    /// <returns>Generated property code.</returns>
    private static string GeneratePartialProperty(PartialPropertyInfo prop, string className)
    {
        var sb = new StringBuilder();
        var batchIdsParam = "";
        if (prop.BatchIds is not null && prop.BatchIds.Length > 0)
        {
            var quotedBatchIds = string.Join(", ", prop.BatchIds.Select(id => $"\"{id}\""));
            batchIdsParam = $", {quotedBatchIds}";
        }

        // Handle required modifier
        var requiredModifier = prop.HasRequiredModifier ? "required " : "";

        // Use Model. prefix for StateHasChanged (component context)
        var qualifiedPropertyName = $"Model.{prop.Name}";

        sb.AppendLine($"    {prop.Accessibility} {requiredModifier}partial {prop.Type} {prop.Name}");
        sb.AppendLine("    {");
        sb.AppendLine("        get => field;");

        // If the property has init accessor, generate with init (even if invalid)
        // The diagnostic will inform the user about invalid usage
        if (prop.HasInitAccessor)
        {
            // For IObservableCollection with init, skip reactive pattern
            // Reactivity comes from observing the collection, not property changes
            if (prop.IsObservableCollection)
            {
                sb.AppendLine("        init => field = value;");
            }
            else
            {
                // For non-IObservableCollection, still generate init but add reactive pattern
                // This maintains compatibility but the diagnostic will warn the user
                sb.AppendLine("        init");
                sb.AppendLine("        {");
                sb.AppendLine("            field = value;");
                sb.AppendLine($"            StateHasChanged(\"{qualifiedPropertyName}\"{batchIdsParam});");
                sb.AppendLine("        }");
            }
        }
        else
        {
            sb.AppendLine("        [UsedImplicitly]");
            sb.AppendLine("        set");
            sb.AppendLine("        {");

            if (prop.IsEquatable)
            {
                sb.AppendLine("            if (field != value)");
                sb.AppendLine("            {");
                sb.AppendLine("                field = value;");
                sb.AppendLine($"                StateHasChanged(\"{qualifiedPropertyName}\"{batchIdsParam});");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine("            field = value;");
                sb.AppendLine($"            StateHasChanged(\"{qualifiedPropertyName}\"{batchIdsParam});");
            }

            sb.AppendLine("        }");
        }

        sb.Append("    }");
        return sb.ToString();
    }

    /// <summary>
    /// Generates the FilterUsedProperties method implementation for model-to-model filtering.
    /// This method checks if any of the given property names match properties used from referenced models.
    /// </summary>
    /// <param name="modelReferences">Collection of model references.</param>
    /// <returns>Generated method code.</returns>
    public static string GenerateFilterUsedPropertiesMethod(IEnumerable<ModelReferenceInfo> modelReferences)
    {
        var sb = new StringBuilder();
        var modelRefsList = modelReferences.ToList();

        sb.AppendLine("    public override bool FilterUsedProperties(params string[] propertyNames)");
        sb.AppendLine("    {");

        sb.AppendLine("        if (propertyNames.Length == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Collect all used properties across all model references
        var allUsedProps = new List<string>();
        foreach (var modelRef in modelRefsList)
        {
            foreach (var usedProp in modelRef.UsedProperties)
            {
                // Transform "IsDay" -> "Model.Settings.IsDay"
                var qualifiedProp = $"Model.{modelRef.PropertyName}.{usedProp}";
                allUsedProps.Add(qualifiedProp);
            }
        }

        // If no model references or no used properties at all, return true (pass through all - no filtering information)
        if (modelRefsList.Count == 0 || allUsedProps.Count == 0)
        {
            sb.AppendLine("        // No filtering information available - pass through all");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            return sb.ToString();
        }

        // Generate the array of used properties
        sb.Append("        var usedProps = new[] { ");
        sb.Append(string.Join(", ", allUsedProps.Select(p => $"\"{p}\"")));
        sb.AppendLine(" };");
        sb.AppendLine();
        sb.AppendLine("        return propertyNames.Intersect(usedProps).Any();");
        sb.AppendLine("    }");

        return sb.ToString();
    }
}
