using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates command property implementations with backing fields and factory calls.
/// </summary>
public static class CommandTemplate
{
    /// <summary>
    /// Generates backing fields for command properties.
    /// </summary>
    /// <param name="commandProperties">Collection of command properties.</param>
    /// <returns>Generated backing fields code.</returns>
    public static string GenerateBackingFields(IEnumerable<CommandPropertyInfo> commandProperties)
    {
        var sb = new StringBuilder();
        foreach (var cmd in commandProperties)
        {
            var backingFieldName = GetBackingFieldName(cmd.Name);
            var concreteType = GetConcreteCommandType(cmd.Type);
            sb.AppendLine($"    private {concreteType} {backingFieldName};");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates command property implementations.
    /// </summary>
    /// <param name="commandProperties">Collection of command properties.</param>
    /// <returns>Generated properties code.</returns>
    public static string GenerateCommandProperties(IEnumerable<CommandPropertyInfo> commandProperties)
    {
        var sb = new StringBuilder();
        foreach (var cmd in commandProperties)
        {
            var backingField = GetBackingFieldName(cmd.Name);
            sb.AppendLine($"    {cmd.Accessibility} partial {cmd.Type} {cmd.Name}");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => {backingField};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates command initializations in constructor.
    /// </summary>
    /// <param name="modelInfo">The model information.</param>
    /// <param name="observableModelAnalyzer">Analyzer to get observed properties.</param>
    /// <returns>Generated initialization code.</returns>
    public static string GenerateCommandInitializations(ObservableModelInfo modelInfo,
        Func<ObservableModelInfo, CommandPropertyInfo, IEnumerable<string>> getObservedProperties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        // Initialize commands");
        foreach (var cmd in modelInfo.CommandProperties)
        {
            var observedProps = getObservedProperties(modelInfo, cmd);
            var observedPropsArray = $"[\"{string.Join("\", \"", observedProps)}\"]";

            var factoryCall = GetFactoryCall(cmd, observedPropsArray);
            var backingField = GetBackingFieldName(cmd.Name);
            sb.AppendLine($"        {backingField} = {factoryCall};");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Generates command trigger subscriptions in constructor.
    /// </summary>
    /// <param name="commandsWithTriggers">Commands that have triggers.</param>
    /// <param name="modelInfo">The model information.</param>
    /// <returns>Generated subscription code.</returns>
    public static string GenerateCommandTriggerSubscriptions(IEnumerable<CommandPropertyInfo> commandsWithTriggers,
        ObservableModelInfo modelInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        // Subscribe to command triggers");
        sb.AppendLine();

        foreach (var cmd in commandsWithTriggers)
        {
            var backingField = GetBackingFieldName(cmd.Name);

            foreach (var trigger in cmd.Triggers)
            {
                var (sourceModel, triggerProperty) = ParseTriggerProperty(trigger.TriggerProperty, modelInfo);
                // Use Model. prefix for all trigger properties
                var qualifiedTriggerProp = string.IsNullOrEmpty(sourceModel)
                    ? $"Model.{triggerProperty}"
                    : $"Model.{sourceModel}.{triggerProperty}";
                var triggerProps = $"[\"{qualifiedTriggerProp}\"]";

                var combinedCondition = GetCombinedTriggerCondition(cmd, trigger);

                if (string.IsNullOrEmpty(sourceModel))
                {
                    // Local property trigger - listen to own Observable
                    sb.AppendLine($"        Subscriptions.Add(Observable.Where(p => p.Intersect({triggerProps}).Any())");
                    if (!string.IsNullOrEmpty(combinedCondition))
                    {
                        sb.AppendLine($"            .Where(_ => {combinedCondition})");
                    }
                    sb.AppendLine(GetTriggerCall(cmd, backingField, trigger.Parameter));
                }
                else
                {
                    // Referenced model property trigger - listen to referenced model's Observable
                    // It emits "Model.X", we need to match "Model.{RefName}.X"
                    var refTriggerProp = $"Model.{triggerProperty}";
                    var refTriggerProps = $"[\"{refTriggerProp}\"]";
                    sb.AppendLine($"        Subscriptions.Add({sourceModel}.Observable.Where(p => p.Intersect({refTriggerProps}).Any())");
                    if (!string.IsNullOrEmpty(combinedCondition))
                    {
                        sb.AppendLine($"            .Where(_ => {combinedCondition})");
                    }
                    sb.AppendLine(GetTriggerCall(cmd, backingField, trigger.Parameter));
                }
            }
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Gets the backing field name for a command property.
    /// </summary>
    private static string GetBackingFieldName(string propertyName)
    {
        return $"_{propertyName.Substring(0, 1).ToLower()}{propertyName.Substring(1)}";
    }

    /// <summary>
    /// Converts interface command type to concrete implementation type.
    /// </summary>
    private static string GetConcreteCommandType(string interfaceType)
    {
        // Convert IObservableCommand -> ObservableCommand
        // Convert IObservableCommandAsync -> ObservableCommandAsync
        // Convert IObservableCommandR -> ObservableCommandR
        // Convert IObservableCommandRAsync -> ObservableCommandRAsync
        // Preserve generic parameters if present
        return interfaceType.Replace("IObservableCommandRAsync", "ObservableCommandRAsync")
            .Replace("IObservableCommandR", "ObservableCommandR")
            .Replace("IObservableCommandAsync", "ObservableCommandAsync")
            .Replace("IObservableCommand", "ObservableCommand");
    }

    /// <summary>
    /// Gets the factory call for command initialization.
    /// </summary>
    private static string GetFactoryCall(CommandPropertyInfo command, string observedPropsArray)
    {
        // Determine factory type based on command property type and method signature
        // IObservableCommandRAsync<T> or IObservableCommandRAsync<T1, T2>
        if (command.Type.Contains("IObservableCommandRAsync<"))
        {
            var genericType = ExtractGenericType(command.Type);
            if (command.SupportsCancellation)
            {
                // Use cancelable factory for methods with CancellationToken
                if (command.CanExecuteMethod is not null)
                {
                    return $"new ObservableCommandRAsyncCancelableFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
                }

                return $"new ObservableCommandRAsyncCancelableFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
            }

            // Use regular async factory for methods without CancellationToken
            if (command.CanExecuteMethod is not null)
            {
                return $"new ObservableCommandRAsyncFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandRAsyncFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        // IObservableCommandAsync<T>
        if (command.Type.Contains("IObservableCommandAsync<"))
        {
            var genericType = ExtractGenericType(command.Type);
            if (command.SupportsCancellation)
            {
                // Use cancelable factory for methods with CancellationToken
                if (command.CanExecuteMethod is not null)
                {
                    return $"new ObservableCommandAsyncCancelableFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
                }

                return $"new ObservableCommandAsyncCancelableFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
            }

            // Use regular async factory for methods without CancellationToken
            if (command.CanExecuteMethod is not null)
            {
                return $"new ObservableCommandAsyncFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandAsyncFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        // IObservableCommandAsync (no parameters)
        if (command.Type.Contains("IObservableCommandAsync"))
        {
            if (command.SupportsCancellation)
            {
                // Use cancelable factory for methods with CancellationToken
                if (command.CanExecuteMethod is not null)
                {
                    return $"new ObservableCommandAsyncCancelableFactory(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
                }

                return $"new ObservableCommandAsyncCancelableFactory(this, {observedPropsArray}, {command.ExecuteMethod})";
            }

            // Use regular async factory for methods without CancellationToken
            if (command.CanExecuteMethod is not null)
            {
                return $"new ObservableCommandAsyncFactory(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandAsyncFactory(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        // IObservableCommandR<T> or IObservableCommandR<T1, T2>
        if (command.Type.Contains("IObservableCommandR<"))
        {
            var genericType = ExtractGenericType(command.Type);
            if (command.CanExecuteMethod is not null)
            {
                return $"new ObservableCommandRFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandRFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        // IObservableCommand<T>
        if (command.Type.Contains("IObservableCommand<"))
        {
            var genericType = ExtractGenericType(command.Type);
            if (command.CanExecuteMethod is not null)
            {
                return $"new ObservableCommandFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        // IObservableCommand (no parameters, no return value)
        if (command.CanExecuteMethod is not null)
        {
            return $"new ObservableCommandFactory(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
        }

        return $"new ObservableCommandFactory(this, {observedPropsArray}, {command.ExecuteMethod})";
    }

    /// <summary>
    /// Gets the trigger call for command execution.
    /// </summary>
    private static string GetTriggerCall(CommandPropertyInfo command, string backingField, string? parameter = null)
    {
        // Determine trigger call based on method signature
        // Async commands (includes IObservableCommandAsync and IObservableCommandRAsync)
        if (command.Type.Contains("IObservableCommandAsync") || command.Type.Contains("IObservableCommandRAsync"))
        {
            // Use SubscribeAwait for async commands
            if (parameter is not null)
            {
                return $"            .SubscribeAwait(async (_, ct) => await {backingField}.ExecuteAsync({parameter}, ct), AwaitOperation.Switch));";
            }

            return $"            .SubscribeAwait(async (_, ct) => await {backingField}.ExecuteAsync(ct), AwaitOperation.Switch));";
        }

        // Sync commands (includes IObservableCommand and IObservableCommandR)
        if (parameter is not null)
        {
            return $"            .Subscribe(_ => {backingField}.Execute({parameter})));";
        }

        return $"            .Subscribe(_ => {backingField}.Execute()));";
    }

    /// <summary>
    /// Extracts the generic type from a command type string.
    /// </summary>
    private static string ExtractGenericType(string type)
    {
        var start = type.IndexOf('<') + 1;
        var end = type.LastIndexOf('>');
        return type.Substring(start, end - start);
    }

    /// <summary>
    /// Parses trigger property into source model and property name.
    /// </summary>
    private static (string sourceModel, string property) ParseTriggerProperty(string triggerProperty, ObservableModelInfo modelInfo)
    {
        // Handle dot notation for referenced model properties (e.g., "ISettingsModel.AutoRefresh")
        if (triggerProperty.Contains('.'))
        {
            var parts = triggerProperty.Split('.');
            if (parts.Length == 2)
            {
                var modelName = parts[0];
                var propertyName = parts[1];

                // Check if this matches a model reference
                var modelRef = modelInfo.ModelReferences.FirstOrDefault(mr =>
                    mr.PropertyName == modelName || mr.ReferencedModelTypeName == modelName);

                if (modelRef is not null)
                {
                    return (modelRef.PropertyName, propertyName);
                }
            }
        }

        // Local property (no dot notation)
        return ("", triggerProperty);
    }

    /// <summary>
    /// Gets the combined trigger condition from command and trigger.
    /// </summary>
    private static string GetCombinedTriggerCondition(CommandPropertyInfo command, CommandTriggerInfo trigger)
    {
        var conditions = new List<string>();

        // Add canExecuteMethod if present
        if (!string.IsNullOrEmpty(command.CanExecuteMethod))
        {
            conditions.Add($"{command.CanExecuteMethod}()");
        }

        // Add canTriggerMethod if present
        if (!string.IsNullOrEmpty(trigger.CanTriggerMethod))
        {
            conditions.Add($"{trigger.CanTriggerMethod}()");
        }

        // Combine with logical AND if multiple conditions exist
        return conditions.Count switch
        {
            0 => "",
            1 => conditions[0],
            _ => string.Join(" && ", conditions)
        };
    }
}
