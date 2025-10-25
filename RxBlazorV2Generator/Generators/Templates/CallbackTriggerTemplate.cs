using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates callback trigger infrastructure for external services to subscribe to property changes.
/// Callback triggers provide a clean API for services to register callbacks that execute when properties change.
/// </summary>
public static class CallbackTriggerTemplate
{
    /// <summary>
    /// Generates callback storage lists for properties with callback triggers.
    /// </summary>
    /// <param name="partialProperties">Collection of partial properties with callback triggers.</param>
    /// <returns>Generated field declarations.</returns>
    public static string GenerateCallbackStorageFields(IEnumerable<PartialPropertyInfo> partialProperties)
    {
        var sb = new StringBuilder();
        var propertiesWithCallbacks = partialProperties.Where(p => p.CallbackTriggers.Any()).ToList();

        if (!propertiesWithCallbacks.Any())
        {
            return string.Empty;
        }

        sb.AppendLine("    // Callback storage for external subscriptions");

        foreach (var prop in propertiesWithCallbacks)
        {
            foreach (var callbackTrigger in prop.CallbackTriggers)
            {
                var callbackType = callbackTrigger.TriggerType == CallbackTriggerType.Sync
                    ? "Action"
                    : "Func<CancellationToken, Task>";

                var fieldName = $"_{char.ToLower(callbackTrigger.MethodName[0])}{callbackTrigger.MethodName.Substring(1)}Callbacks";

                sb.AppendLine($"    private readonly List<{callbackType}> {fieldName} = new();");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates public registration methods for external services to subscribe to property changes.
    /// </summary>
    /// <param name="partialProperties">Collection of partial properties with callback triggers.</param>
    /// <returns>Generated registration methods.</returns>
    public static string GenerateCallbackRegistrationMethods(IEnumerable<PartialPropertyInfo> partialProperties)
    {
        var sb = new StringBuilder();
        var propertiesWithCallbacks = partialProperties.Where(p => p.CallbackTriggers.Any()).ToList();

        if (!propertiesWithCallbacks.Any())
        {
            return string.Empty;
        }

        sb.AppendLine();
        sb.AppendLine("    // Callback registration methods for external subscriptions");

        foreach (var prop in propertiesWithCallbacks)
        {
            foreach (var callbackTrigger in prop.CallbackTriggers)
            {
                var callbackType = callbackTrigger.TriggerType == CallbackTriggerType.Sync
                    ? "Action"
                    : "Func<CancellationToken, Task>";

                var fieldName = $"_{char.ToLower(callbackTrigger.MethodName[0])}{callbackTrigger.MethodName.Substring(1)}Callbacks";

                sb.AppendLine();
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Registers a callback to be invoked when the {prop.Name} property changes.");
                if (callbackTrigger.TriggerType == CallbackTriggerType.Async)
                {
                    sb.AppendLine($"    /// The callback receives a CancellationToken for async operations.");
                }
                sb.AppendLine($"    /// Subscriptions are automatically disposed when the model is disposed.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    /// <param name=\"callback\">The callback to invoke on property changes.</param>");
                sb.AppendLine($"    public void {callbackTrigger.MethodName}({callbackType} callback)");
                sb.AppendLine("    {");
                sb.AppendLine($"        if (callback is null)");
                sb.AppendLine("        {");
                sb.AppendLine($"            throw new ArgumentNullException(nameof(callback));");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        {fieldName}.Add(callback);");
                sb.AppendLine("    }");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates Observable subscriptions in constructor that invoke all registered callbacks.
    /// </summary>
    /// <param name="partialProperties">Collection of partial properties with callback triggers.</param>
    /// <returns>Generated subscription code.</returns>
    public static string GenerateCallbackSubscriptions(IEnumerable<PartialPropertyInfo> partialProperties)
    {
        var sb = new StringBuilder();
        var propertiesWithCallbacks = partialProperties.Where(p => p.CallbackTriggers.Any()).ToList();

        if (!propertiesWithCallbacks.Any())
        {
            return string.Empty;
        }

        sb.AppendLine("        // Subscribe callback triggers for external subscriptions");
        sb.AppendLine();

        foreach (var prop in propertiesWithCallbacks)
        {
            // Group sync and async triggers together for the same property to create a single subscription
            var syncTriggers = prop.CallbackTriggers.Where(t => t.TriggerType == CallbackTriggerType.Sync).ToList();
            var asyncTriggers = prop.CallbackTriggers.Where(t => t.TriggerType == CallbackTriggerType.Async).ToList();

            var qualifiedPropertyName = $"Model.{prop.Name}";
            var propertyNameArray = $"[\"{qualifiedPropertyName}\"]";

            if (syncTriggers.Any())
            {
                sb.AppendLine($"        // Sync callbacks for {prop.Name}");
                sb.AppendLine($"        Subscriptions.Add(Observable.Where(p => p.Intersect({propertyNameArray}).Any())");
                sb.AppendLine("            .Subscribe(_ =>");
                sb.AppendLine("            {");

                foreach (var trigger in syncTriggers)
                {
                    var fieldName = $"_{char.ToLower(trigger.MethodName[0])}{trigger.MethodName.Substring(1)}Callbacks";
                    sb.AppendLine($"                foreach (var callback in {fieldName})");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    callback();");
                    sb.AppendLine("                }");
                }

                sb.AppendLine("            }));");
                sb.AppendLine();
            }

            if (asyncTriggers.Any())
            {
                sb.AppendLine($"        // Async callbacks for {prop.Name}");
                sb.AppendLine($"        Subscriptions.Add(Observable.Where(p => p.Intersect({propertyNameArray}).Any())");
                sb.AppendLine("            .SubscribeAwait(async (_, ct) =>");
                sb.AppendLine("            {");

                foreach (var trigger in asyncTriggers)
                {
                    var fieldName = $"_{char.ToLower(trigger.MethodName[0])}{trigger.MethodName.Substring(1)}Callbacks";
                    sb.AppendLine($"                foreach (var callback in {fieldName})");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    await callback(ct);");
                    sb.AppendLine("                }");
                }

                sb.AppendLine("            }, AwaitOperation.Switch));");
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }
}
