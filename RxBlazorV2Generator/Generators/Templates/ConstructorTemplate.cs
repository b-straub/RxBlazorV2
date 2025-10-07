using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates constructor implementations with dependency injection and initialization logic.
/// </summary>
public static class ConstructorTemplate
{
    /// <summary>
    /// Generates constructor with parameters for model references and DI fields.
    /// </summary>
    /// <param name="modelInfo">The model information.</param>
    /// <param name="getObservedProperties">Function to get observed properties for commands.</param>
    /// <returns>Generated constructor code or empty string if no constructor needed.</returns>
    public static string GenerateConstructor(ObservableModelInfo modelInfo,
        Func<ObservableModelInfo, CommandPropertyInfo, IEnumerable<string>> getObservedProperties)
    {
        var hasObservableCollections = modelInfo.PartialProperties.Any(p => p.IsObservableCollection);

        if (modelInfo.ModelReferences.Any() || modelInfo.DIFields.Any() || hasObservableCollections)
        {
            return GenerateConstructorWithDependencies(modelInfo, getObservedProperties, hasObservableCollections);
        }

        if (modelInfo.CommandProperties.Any())
        {
            return GenerateConstructorWithCommandsOnly(modelInfo, getObservedProperties, hasObservableCollections);
        }

        return string.Empty;
    }

    /// <summary>
    /// Generates constructor with dependencies (model references and/or DI fields).
    /// </summary>
    private static string GenerateConstructorWithDependencies(ObservableModelInfo modelInfo,
        Func<ObservableModelInfo, CommandPropertyInfo, IEnumerable<string>> getObservedProperties,
        bool hasObservableCollections)
    {
        var sb = new StringBuilder();

        // Generate constructor parameters
        var constructorParams = new List<string>();

        // Add model reference parameters
        constructorParams.AddRange(modelInfo.ModelReferences.Select(mr =>
            $"{mr.ReferencedModelTypeName} {mr.PropertyName.ToLowerInvariant()}"));

        // Add DI field parameters
        constructorParams.AddRange(modelInfo.DIFields.Select(df =>
        {
            var paramName = df.FieldName.StartsWith("_") ? df.FieldName.Substring(1) : df.FieldName;
            return $"{df.FieldType} {paramName.ToLowerInvariant()}";
        }));

        var allParams = string.Join(", ", constructorParams);

        sb.AppendLine($"    public {modelInfo.ClassName}({allParams}) : base()");
        sb.AppendLine("    {");

        // Assign referenced models
        foreach (var modelRef in modelInfo.ModelReferences)
        {
            sb.AppendLine($"        {modelRef.PropertyName} = {modelRef.PropertyName.ToLowerInvariant()};");
        }

        // Assign DI fields
        foreach (var diField in modelInfo.DIFields)
        {
            var paramName = diField.FieldName.StartsWith("_") ? diField.FieldName.Substring(1) : diField.FieldName;
            sb.AppendLine($"        {diField.FieldName} = {paramName.ToLowerInvariant()};");
        }

        // Initialize IObservableCollection properties
        if (hasObservableCollections)
        {
            sb.AppendLine();
            sb.AppendLine(GenerateObservableCollectionInitializations(modelInfo));
        }

        // Initialize commands
        if (modelInfo.CommandProperties.Any())
        {
            sb.AppendLine();
            sb.AppendLine(CommandTemplate.GenerateCommandInitializations(modelInfo, getObservedProperties));
        }

        // Generate observable subscriptions for referenced model changes
        if (modelInfo.ModelReferences.Any())
        {
            sb.AppendLine();
            sb.AppendLine(GenerateModelReferenceSubscriptions(modelInfo));
        }

        // Generate command trigger subscriptions
        var commandsWithTriggers = modelInfo.CommandProperties.Where(cmd => cmd.Triggers.Any()).ToList();
        if (commandsWithTriggers.Any())
        {
            sb.AppendLine();
            sb.AppendLine(CommandTemplate.GenerateCommandTriggerSubscriptions(commandsWithTriggers, modelInfo));
        }

        sb.AppendLine("    }");

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates constructor for models without dependencies but with commands.
    /// </summary>
    private static string GenerateConstructorWithCommandsOnly(ObservableModelInfo modelInfo,
        Func<ObservableModelInfo, CommandPropertyInfo, IEnumerable<string>> getObservedProperties,
        bool hasObservableCollections)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"    public {modelInfo.ClassName}() : base()");
        sb.AppendLine("    {");

        // Initialize IObservableCollection properties
        if (hasObservableCollections)
        {
            sb.AppendLine(GenerateObservableCollectionInitializations(modelInfo));
            sb.AppendLine();
        }

        // Initialize commands
        sb.AppendLine(CommandTemplate.GenerateCommandInitializations(modelInfo, getObservedProperties));

        // Generate command trigger subscriptions for models without dependencies
        var commandsWithTriggers = modelInfo.CommandProperties.Where(cmd => cmd.Triggers.Any()).ToList();
        if (commandsWithTriggers.Any())
        {
            sb.AppendLine();
            sb.AppendLine(CommandTemplate.GenerateCommandTriggerSubscriptions(commandsWithTriggers, modelInfo));
        }

        sb.AppendLine("    }");

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates observable collection initializations.
    /// </summary>
    private static string GenerateObservableCollectionInitializations(ObservableModelInfo modelInfo)
    {
        var sb = new StringBuilder();
        var observableCollectionProperties = modelInfo.PartialProperties.Where(p => p.IsObservableCollection).ToList();

        if (observableCollectionProperties.Any())
        {
            sb.AppendLine("        // Initialize IObservableCollection properties");
            foreach (var prop in observableCollectionProperties)
            {
                var batchIdsParam = "";
                if (prop.BatchIds is not null && prop.BatchIds.Length > 0)
                {
                    var quotedBatchIds = string.Join(", ", prop.BatchIds.Select(id => $"\"{id}\""));
                    batchIdsParam = $", {quotedBatchIds}";
                }

                sb.AppendLine($"        {prop.Name} = new();");
                sb.AppendLine($"        _subscriptions.Add({prop.Name}.ObserveChanged()");
                sb.AppendLine($"            .Subscribe(_ => StateHasChanged(\"{prop.Name}\"{batchIdsParam})));");
                if (prop != observableCollectionProperties.Last())
                {
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates subscriptions for referenced model changes.
    /// </summary>
    private static string GenerateModelReferenceSubscriptions(ObservableModelInfo modelInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        // Subscribe to referenced model changes");

        foreach (var modelRef in modelInfo.ModelReferences)
        {
            var observedProps = $"[\"{string.Join("\", \"", modelRef.UsedProperties)}\"]";
            sb.AppendLine($"        _subscriptions.Add({modelRef.PropertyName}.Observable.Select(props => props.Intersect({observedProps})).Where(props => props.Any())");
            sb.AppendLine("            .Subscribe(props => StateHasChanged(props.ToArray())));");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }
}
