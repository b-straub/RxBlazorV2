using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates OnContextReadyIntern() method for subscribing to model observer methods in services.
/// Model observers are methods in service classes decorated with [ObservableModelObserver] that
/// automatically subscribe to property changes in the model.
/// </summary>
public static class ModelObserverTemplate
{
    /// <summary>
    /// Generates the OnContextReadyIntern() override method with subscriptions for all model observers.
    /// </summary>
    /// <param name="modelObservers">Collection of model observer information from service classes.</param>
    /// <returns>Generated OnContextReadyIntern() method code, or empty string if no observers.</returns>
    public static string GenerateOnContextReadyIntern(IEnumerable<ModelObserverInfo> modelObservers)
    {
        var observersList = modelObservers.ToList();

        if (!observersList.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine("    protected override void OnContextReadyIntern()");
        sb.AppendLine("    {");

        // Group observers by property name for comments
        var observersByProperty = observersList.GroupBy(o => o.PropertyName).ToList();

        foreach (var propertyGroup in observersByProperty)
        {
            var propertyName = propertyGroup.Key;
            var qualifiedPropertyName = $"Model.{propertyName}";
            var propertyNameArray = $"[\"{qualifiedPropertyName}\"]";

            foreach (var observer in propertyGroup)
            {
                if (observer.IsAsync)
                {
                    GenerateAsyncObserverSubscription(sb, observer, propertyNameArray);
                }
                else
                {
                    GenerateSyncObserverSubscription(sb, observer, propertyNameArray);
                }

                sb.AppendLine();
            }
        }

        // Close method
        sb.AppendLine("    }");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a synchronous observer subscription.
    /// Pattern: .Subscribe(_ => Service.Method(this))
    /// </summary>
    private static void GenerateSyncObserverSubscription(
        StringBuilder sb,
        ModelObserverInfo observer,
        string propertyNameArray)
    {
        sb.AppendLine($"        Subscriptions.Add(Observable.Where(p => p.Intersect({propertyNameArray}).Any())");
        sb.AppendLine($"            .Subscribe(_ => {observer.ServiceFieldName}.{observer.MethodName}(this)));");
    }

    /// <summary>
    /// Generates an asynchronous observer subscription.
    /// Pattern with CancellationToken: .SubscribeAwait(async (_, ct) => await Service.Method(this, ct), AwaitOperation.Switch)
    /// Pattern without CancellationToken: .SubscribeAwait(async (_, _) => await Service.Method(this), AwaitOperation.Switch)
    /// </summary>
    private static void GenerateAsyncObserverSubscription(
        StringBuilder sb,
        ModelObserverInfo observer,
        string propertyNameArray)
    {
        sb.AppendLine($"        Subscriptions.Add(Observable.Where(p => p.Intersect({propertyNameArray}).Any())");

        if (observer.HasCancellationToken)
        {
            sb.AppendLine("            .SubscribeAwait(async (_, ct) =>");
            sb.AppendLine("            {");
            sb.AppendLine($"                await {observer.ServiceFieldName}.{observer.MethodName}(this, ct);");
            sb.AppendLine("            }, AwaitOperation.Switch));");
        }
        else
        {
            sb.AppendLine("            .SubscribeAwait(async (_, _) =>");
            sb.AppendLine("            {");
            sb.AppendLine($"                await {observer.ServiceFieldName}.{observer.MethodName}(this);");
            sb.AppendLine("            }, AwaitOperation.Switch));");
        }
    }

    /// <summary>
    /// Checks if any model observers are present that would require generating OnContextReadyIntern().
    /// </summary>
    /// <param name="modelObservers">Collection of model observer information.</param>
    /// <returns>True if there are observers to generate subscriptions for.</returns>
    public static bool HasModelObservers(IEnumerable<ModelObserverInfo> modelObservers)
    {
        return modelObservers.Any();
    }
}
