using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates OnContextReadyIntern() and OnContextReadyInternAsync() methods for:
/// 1. Calling ContextReady()/ContextReadyAsync() on referenced ObservableModel dependencies
/// 2. Subscribing to model observer methods in services decorated with [ObservableModelObserver]
/// </summary>
public static class ModelObserverTemplate
{
    /// <summary>
    /// Generates the OnContextReadyIntern() override method that:
    /// 1. Calls ContextReady() on all referenced ObservableModel dependencies
    /// 2. Sets up subscriptions for model observers in services
    /// </summary>
    /// <param name="modelReferences">Collection of referenced ObservableModel dependencies.</param>
    /// <param name="modelObservers">Collection of model observer information from service classes.</param>
    /// <returns>Generated OnContextReadyIntern() method code, or empty string if nothing to generate.</returns>
    public static string GenerateOnContextReadyIntern(
        IEnumerable<ModelReferenceInfo> modelReferences,
        IEnumerable<ModelObserverInfo> modelObservers)
    {
        var referencesList = modelReferences.ToList();
        var observersList = modelObservers.ToList();

        if (!referencesList.Any() && !observersList.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine();
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

        // Call ContextReady() on all referenced ObservableModel dependencies
        if (referencesList.Any())
        {
            sb.AppendLine("        // Initialize referenced ObservableModel dependencies");
            foreach (var reference in referencesList)
            {
                sb.AppendLine($"        {reference.PropertyName}.ContextReady();");
            }
            sb.AppendLine();
        }

        // Generate subscriptions for model observers
        if (observersList.Any())
        {
            sb.AppendLine("        // Subscribe to model observers in services");
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
        }

        // Close method
        sb.AppendLine("    }");

        return sb.ToString();
    }

    /// <summary>
    /// Generates the OnContextReadyInternAsync() override method that calls ContextReadyAsync()
    /// on all referenced ObservableModel dependencies.
    /// </summary>
    /// <param name="modelReferences">Collection of referenced ObservableModel dependencies.</param>
    /// <returns>Generated OnContextReadyInternAsync() method code, or empty string if no references.</returns>
    public static string GenerateOnContextReadyInternAsync(IEnumerable<ModelReferenceInfo> modelReferences)
    {
        var referencesList = modelReferences.ToList();

        if (!referencesList.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine();
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
        sb.AppendLine("        // Initialize referenced ObservableModel dependencies (async)");

        foreach (var reference in referencesList)
        {
            sb.AppendLine($"        await {reference.PropertyName}.ContextReadyAsync();");
        }

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
    /// Checks if OnContextReadyIntern() generation is needed.
    /// </summary>
    /// <param name="modelReferences">Collection of referenced ObservableModel dependencies.</param>
    /// <param name="modelObservers">Collection of model observer information.</param>
    /// <returns>True if there are references or observers requiring generation.</returns>
    public static bool RequiresOnContextReadyIntern(
        IEnumerable<ModelReferenceInfo> modelReferences,
        IEnumerable<ModelObserverInfo> modelObservers)
    {
        return modelReferences.Any() || modelObservers.Any();
    }

    /// <summary>
    /// Checks if OnContextReadyInternAsync() generation is needed.
    /// </summary>
    /// <param name="modelReferences">Collection of referenced ObservableModel dependencies.</param>
    /// <returns>True if there are references requiring async generation.</returns>
    public static bool RequiresOnContextReadyInternAsync(IEnumerable<ModelReferenceInfo> modelReferences)
    {
        return modelReferences.Any();
    }
}
