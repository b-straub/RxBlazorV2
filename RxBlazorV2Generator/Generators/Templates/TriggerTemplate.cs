using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates property trigger subscriptions that directly call methods when properties change.
/// Property triggers execute methods automatically without creating command objects.
/// </summary>
public static class TriggerTemplate
{
    /// <summary>
    /// Generates trigger subscriptions in constructor that directly call methods.
    /// </summary>
    /// <param name="partialProperties">Collection of partial properties with triggers.</param>
    /// <returns>Generated subscription code.</returns>
    public static string GenerateTriggerSubscriptions(IEnumerable<PartialPropertyInfo> partialProperties)
    {
        var sb = new StringBuilder();
        var propertiesWithTriggers = partialProperties.Where(p => p.Triggers.Any()).ToList();

        if (!propertiesWithTriggers.Any())
        {
            return string.Empty;
        }

        sb.AppendLine("        // Subscribe property triggers");
        sb.AppendLine();

        foreach (var prop in propertiesWithTriggers)
        {
            foreach (var trigger in prop.Triggers)
            {
                // Use qualified property name (Model.PropertyName) to match filter system
                var qualifiedPropertyName = $"Model.{prop.Name}";
                var propertyNameArray = $"[\"{qualifiedPropertyName}\"]";

                // Generate the subscription
                sb.AppendLine($"        Subscriptions.Add(Observable.Where(p => p.Intersect({propertyNameArray}).Any())");

                // Add canTrigger condition if present
                if (!string.IsNullOrEmpty(trigger.CanTriggerMethod))
                {
                    sb.AppendLine($"            .Where(_ => {trigger.CanTriggerMethod}())");
                }

                // Generate direct method call based on async/sync and parameter
                var methodCall = GetDirectMethodCall(trigger);
                sb.AppendLine(methodCall);
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Gets the direct method call for property trigger execution.
    /// Reuses the same logic as command triggers for consistency.
    /// </summary>
    private static string GetDirectMethodCall(PropertyTriggerInfo trigger)
    {
        // Check if method is async (either has cancellation token or method name ends with Async)
        var isAsync = trigger.SupportsCancellation || trigger.ExecuteMethod.EndsWith("Async");

        if (isAsync)
        {
            // Use SubscribeAwait for async methods
            if (!string.IsNullOrEmpty(trigger.Parameter))
            {
                // Async with parameter - check if method actually takes CancellationToken
                if (trigger.SupportsCancellation)
                {
                    return $"            .SubscribeAwait(async (_, ct) => await {trigger.ExecuteMethod}({trigger.Parameter}, ct), AwaitOperation.Switch));";
                }
                else
                {
                    return $"            .SubscribeAwait(async (_, ct) => await {trigger.ExecuteMethod}({trigger.Parameter}), AwaitOperation.Switch));";
                }
            }

            // Async without parameter
            if (trigger.SupportsCancellation)
            {
                return $"            .SubscribeAwait(async (_, ct) => await {trigger.ExecuteMethod}(ct), AwaitOperation.Switch));";
            }

            return $"            .SubscribeAwait(async (_, ct) => await {trigger.ExecuteMethod}(), AwaitOperation.Switch));";
        }

        // Use Subscribe for sync methods
        if (!string.IsNullOrEmpty(trigger.Parameter))
        {
            // Sync with parameter
            return $"            .Subscribe(_ => {trigger.ExecuteMethod}({trigger.Parameter})));";
        }

        // Sync without parameter
        return $"            .Subscribe(_ => {trigger.ExecuteMethod}()));";
    }
}
