#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Generates a callback registration method that external services can use to subscribe to property changes.
/// Provides a clean alternative to manual Observable.Where() subscriptions.
/// Multiple external callbacks can be registered for the same property.
/// </summary>
/// <param name="methodName">
/// Optional. The name of the registration method to generate.
/// If not specified, defaults to On{PropertyName}Changed.
/// Use <see langword="nameof"/> for compile-time safety when specifying.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties in ObservableModel classes.</para>
/// <para><b>Example - Generated Method:</b></para>
/// <code>
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class StatusModel : ObservableModel
/// {
///     [ObservableCallbackTrigger] // Generates OnCurrentUserChanged(Action callback)
///     public partial ClaimsPrincipal? CurrentUser { get; set; }
///
///     [ObservableCallbackTrigger("HandleThemeUpdate")] // Custom method name
///     public partial string Theme { get; set; }
/// }
///
/// // Generated in StatusModel:
/// public void OnCurrentUserChanged(Action callback)
/// {
///     // Internally subscribes to Observable and invokes callback on changes
/// }
/// </code>
/// <para><b>Example - Service Usage:</b></para>
/// <code>
/// public class MyAuthService
/// {
///     private AuthenticationState _authenticationState;
///
///     public MyAuthService(StatusModel statusModel)
///     {
///         // Clean subscription instead of manual Observable.Where()
///         statusModel.OnCurrentUserChanged(() =>
///         {
///             _authenticationState = new AuthenticationState(
///                 statusModel.CurrentUser ?? new ClaimsPrincipal());
///             NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
///         });
///     }
/// }
///
/// // Replaces verbose manual subscription:
/// // _subscription = statusModel.Observable
/// //     .Where(c => c.Contains("Model." + nameof(statusModel.CurrentUser)))
/// //     .Subscribe(_ => { /* handler */ });
/// </code>
/// <para><b>Lifecycle Management:</b></para>
/// <list type="bullet">
/// <item>Subscriptions are automatically managed by the model's CompositeDisposable</item>
/// <item>All callbacks are disposed when the model is disposed</item>
/// <item>Services should handle graceful disposal if disposed before the model</item>
/// </list>
/// <para><b>Thread Safety:</b></para>
/// <para>Callbacks are invoked on the Observable's scheduler thread. Ensure callback implementations are thread-safe or marshal to appropriate context.</para>
/// <para><b>Warning:</b> Avoid circular updates where callbacks modify the same property they're listening to.</para>
/// <para><b>See Also:</b></para>
/// <list type="bullet">
/// <item><see cref="ObservableCallbackTriggerAsyncAttribute"/> - For async callbacks with CancellationToken</item>
/// <item><see cref="ObservableComponentTriggerAttribute"/> - For component-level property change hooks</item>
/// <item><see cref="ObservableTriggerAttribute"/> - For internal model method triggers</item>
/// </list>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ObservableCallbackTriggerAttribute(string? methodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
