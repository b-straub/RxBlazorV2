using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Extensions;
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
        var hasPropertyTriggers = modelInfo.PartialProperties.Any(p => p.Triggers.Any());

        if (modelInfo.ModelReferences.Any() || modelInfo.DIFields.Any() || hasObservableCollections)
        {
            return GenerateConstructorWithDependencies(modelInfo, getObservedProperties, hasObservableCollections);
        }

        if (modelInfo.CommandProperties.Any() || hasPropertyTriggers)
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
            $"{mr.ReferencedModelTypeName} {mr.PropertyName.ToCamelCase()}"));

        // Add DI field parameters (FieldName is now a PascalCase property name)
        constructorParams.AddRange(modelInfo.DIFields.Select(df =>
            $"{df.FieldType} {df.FieldName.ToCamelCase()}"));

        var allParams = string.Join(", ", constructorParams);

        // Don't call : base() for derived models (those that inherit from another ObservableModel)
        // The base class constructor will be called implicitly
        var baseCall = string.IsNullOrEmpty(modelInfo.BaseModelTypeName) ? " : base()" : "";

        // Only use partial keyword if there are actual constructor parameters
        // Models with only observable collections/commands don't need partial constructors
        var partialKeyword = constructorParams.Any() ? "partial " : "";
        sb.AppendLine($"    {modelInfo.ConstructorAccessibility} {partialKeyword}{modelInfo.ClassName}({allParams}){baseCall}");
        sb.AppendLine("    {");

        // Assign referenced models
        foreach (var modelRef in modelInfo.ModelReferences)
        {
            sb.AppendLine($"        {modelRef.PropertyName} = {modelRef.PropertyName.ToCamelCase()};");
        }

        // Assign DI fields (FieldName is now a PascalCase property name)
        foreach (var diField in modelInfo.DIFields)
        {
            sb.AppendLine($"        {diField.FieldName} = {diField.FieldName.ToCamelCase()};");
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

        // Generate property trigger subscriptions (direct method calls, no backing fields)
        var propertiesWithTriggers = modelInfo.PartialProperties.Where(p => p.Triggers.Any()).ToList();
        if (propertiesWithTriggers.Any())
        {
            sb.AppendLine();
            sb.AppendLine(TriggerTemplate.GenerateTriggerSubscriptions(propertiesWithTriggers));
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

        // Don't call : base() for derived models (those that inherit from another ObservableModel)
        // The base class constructor will be called implicitly
        var baseCall = string.IsNullOrEmpty(modelInfo.BaseModelTypeName) ? " : base()" : "";
        // For parameterless constructors (only commands/collections), don't use partial keyword
        sb.AppendLine($"    {modelInfo.ConstructorAccessibility} {modelInfo.ClassName}(){baseCall}");
        sb.AppendLine("    {");

        // Initialize IObservableCollection properties
        if (hasObservableCollections)
        {
            sb.AppendLine(GenerateObservableCollectionInitializations(modelInfo));
            sb.AppendLine();
        }

        // Initialize commands
        if (modelInfo.CommandProperties.Any())
        {
            sb.AppendLine(CommandTemplate.GenerateCommandInitializations(modelInfo, getObservedProperties));
        }

        // Generate command trigger subscriptions for models without dependencies
        var commandsWithTriggers = modelInfo.CommandProperties.Where(cmd => cmd.Triggers.Any()).ToList();
        if (commandsWithTriggers.Any())
        {
            if (modelInfo.CommandProperties.Any() || hasObservableCollections)
            {
                sb.AppendLine();
            }
            sb.AppendLine(CommandTemplate.GenerateCommandTriggerSubscriptions(commandsWithTriggers, modelInfo));
        }

        // Generate property trigger subscriptions (direct method calls, no backing fields)
        var propertiesWithTriggers = modelInfo.PartialProperties.Where(p => p.Triggers.Any()).ToList();
        if (propertiesWithTriggers.Any())
        {
            if (modelInfo.CommandProperties.Any() || commandsWithTriggers.Any() || hasObservableCollections)
            {
                sb.AppendLine();
            }
            sb.AppendLine(TriggerTemplate.GenerateTriggerSubscriptions(propertiesWithTriggers));
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
                sb.AppendLine($"        Subscriptions.Add({prop.Name}.ObserveChanged()");
                sb.AppendLine($"            .Subscribe(_ => StateHasChanged(\"Model.{prop.Name}\"{batchIdsParam})));");
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
    /// Transforms referenced model's "Model.X" emissions to "Model.{RefName}.X" for this model's context.
    /// </summary>
    private static string GenerateModelReferenceSubscriptions(ObservableModelInfo modelInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        // Subscribe to referenced model changes");
        sb.AppendLine("        // Transform referenced model property names: Model.X -> Model.{RefName}.X");

        foreach (var modelRef in modelInfo.ModelReferences)
        {
            // Filter for properties we care about (e.g., ["Model.IsDay", "Model.AutoRefresh"])
            var filterProps = modelRef.UsedProperties.Select(p => $"Model.{p}").ToList();
            var filterArray = $"[\"{string.Join("\", \"", filterProps)}\"]";

            // Transform "Model.IsDay" -> "Model.Settings.IsDay"
            var transformedPrefix = $"Model.{modelRef.PropertyName}.";

            sb.AppendLine($"        Subscriptions.Add({modelRef.PropertyName}.Observable");
            sb.AppendLine($"            .Select(props => props.Intersect({filterArray}).Select(p => p.Replace(\"Model.\", \"{transformedPrefix}\")).ToArray())");
            sb.AppendLine($"            .Where(transformed => transformed.Length > 0)");
            sb.AppendLine("            .Subscribe(props => StateHasChanged(props)));");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }
}
