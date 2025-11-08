using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Generates an async hook method that is called when the property value changes.
/// The hook method receives a CancellationToken parameter.
/// Use on partial properties within ObservableModel classes that have [ObservableComponent] attribute.
/// </summary>
/// <param name="type">
/// Specifies the trigger behavior. Defaults to <see cref="ComponentTriggerType.RenderAndHook"/>.
/// <list type="bullet">
/// <item><see cref="ComponentTriggerType.RenderAndHook"/> - Calls async hook AND re-renders component (default)</item>
/// <item><see cref="ComponentTriggerType.HookOnly"/> - Calls async hook but does NOT re-render component</item>
/// <item><see cref="ComponentTriggerType.RenderOnly"/> - Re-renders component but does NOT generate/call async hook</item>
/// </list>
/// </param>
/// <param name="hookMethodName">Optional custom name for the async hook method.
/// If not specified, defaults to On{PropertyName}ChangedAsync.
/// Only applicable when type is <see cref="ComponentTriggerType.RenderAndHook"/> or <see cref="ComponentTriggerType.HookOnly"/>.</param>
/// <example>
/// [ObservableComponentTriggerAsync]
/// public partial bool IsLoading { get; set; }
/// // Generates: protected virtual Task OnIsLoadingChangedAsync(CancellationToken ct)
///
/// [ObservableComponentTriggerAsync("HandleStateChange")]
/// public partial bool IsActive { get; set; }
/// // Generates: protected virtual Task HandleStateChange(CancellationToken ct)
///
/// [ObservableComponentTriggerAsync(ComponentTriggerType.HookOnly)]
/// public partial string BackgroundData { get; set; }
/// // Hook is called but component does NOT re-render automatically
/// </example>
#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableComponentTriggerAsyncAttribute(ComponentTriggerType type = ComponentTriggerType.RenderAndHook, string? hookMethodName = null) : Attribute
{
}
#pragma warning restore CS9113