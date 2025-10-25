#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Generates an async callback registration method that external services can use to subscribe to property changes.
/// The callback receives a CancellationToken parameter for proper async cancellation support.
/// Provides a clean alternative to manual Observable.Where().SubscribeAwait() subscriptions.
/// Multiple external async callbacks can be registered for the same property.
/// </summary>
/// <param name="methodName">
/// Optional. The name of the async registration method to generate.
/// If not specified, defaults to On{PropertyName}ChangedAsync.
/// Use <see langword="nameof"/> for compile-time safety when specifying.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties in ObservableModel classes.</para>
/// <para><b>Example - Generated Method:</b></para>
/// <code>
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class StatusModel : ObservableModel
/// {
///     [ObservableCallbackTriggerAsync] // Generates OnSettingsChangedAsync(Func&lt;CancellationToken, Task&gt; callback)
///     public partial UserSettings? Settings { get; set; }
///
///     [ObservableCallbackTriggerAsync("HandleConfigUpdateAsync")] // Custom method name
///     public partial AppConfig Config { get; set; }
/// }
///
/// // Generated in StatusModel:
/// public void OnSettingsChangedAsync(Func&lt;CancellationToken, Task&gt; callback)
/// {
///     // Internally subscribes to Observable and invokes async callback on changes
/// }
/// </code>
/// <para><b>Example - Service Usage:</b></para>
/// <code>
/// public class ConfigurationService
/// {
///     private readonly ILogger _logger;
///
///     public ConfigurationService(StatusModel statusModel, ILogger logger)
///     {
///         _logger = logger;
///
///         // Clean async subscription with CancellationToken support
///         statusModel.OnSettingsChangedAsync(async (ct) =>
///         {
///             await SaveSettingsToFileAsync(statusModel.Settings, ct);
///             await _logger.LogAsync("Settings updated", ct);
///         });
///     }
/// }
///
/// // Replaces verbose manual subscription:
/// // _subscription = statusModel.Observable
/// //     .Where(c => c.Contains("Model." + nameof(statusModel.Settings)))
/// //     .SubscribeAwait(async (_, ct) => { /* async handler */ });
/// </code>
/// <para><b>Lifecycle Management:</b></para>
/// <list type="bullet">
/// <item>Async subscriptions are automatically managed by the model's CompositeDisposable</item>
/// <item>All callbacks are disposed when the model is disposed</item>
/// <item>CancellationToken allows proper cancellation of async operations</item>
/// <item>Services should handle graceful disposal if disposed before the model</item>
/// </list>
/// <para><b>Thread Safety:</b></para>
/// <para>Async callbacks are invoked on the Observable's scheduler thread. The CancellationToken enables cooperative cancellation of long-running async operations.</para>
/// <para><b>Warning:</b> Avoid circular updates where callbacks modify the same property they're listening to.</para>
/// <para><b>See Also:</b></para>
/// <list type="bullet">
/// <item><see cref="ObservableCallbackTriggerAttribute"/> - For sync callbacks without CancellationToken</item>
/// <item><see cref="ObservableComponentTriggerAsyncAttribute"/> - For async component-level hooks</item>
/// <item><see cref="ObservableTriggerAttribute"/> - For internal model method triggers</item>
/// </list>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ObservableCallbackTriggerAsyncAttribute(string? methodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
