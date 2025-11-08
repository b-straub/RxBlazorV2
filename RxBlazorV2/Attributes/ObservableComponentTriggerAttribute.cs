#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Generates virtual hook methods in the component class for property change events.
/// Use with [ObservableComponent] attribute to create component-level property change handlers.
/// Multiple triggers can be applied to the same property.
/// When applied to properties in models that are referenced by other models (via partial constructor),
/// the hook methods are automatically propagated to the referencing model's component.
/// </summary>
/// <param name="type">
/// Specifies the trigger behavior. Defaults to <see cref="ComponentTriggerType.RenderAndHook"/>.
/// <list type="bullet">
/// <item><see cref="ComponentTriggerType.RenderAndHook"/> - Calls hook AND re-renders component (default)</item>
/// <item><see cref="ComponentTriggerType.HookOnly"/> - Calls hook but does NOT re-render component</item>
/// <item><see cref="ComponentTriggerType.RenderOnly"/> - Re-renders component but does NOT generate/call hook</item>
/// </list>
/// </param>
/// <param name="hookMethodName">
/// Optional. The name of the hook method to generate.
/// If not specified, defaults to On{PropertyName}Changed.
/// Use <see langword="nameof"/> for compile-time safety when specifying.
/// Only applicable when type is <see cref="ComponentTriggerType.RenderAndHook"/> or <see cref="ComponentTriggerType.HookOnly"/>.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties in models decorated with [ObservableComponent].</para>
/// <para><b>Example - Local Trigger:</b></para>
/// <code>
/// [ObservableComponent]
/// public partial class TestModel : ObservableModel
/// {
///     [ObservableComponentTrigger] // Generates OnNotInBatchChanged
///     public partial int NotInBatch { get; set; }
///
///     [ObservableComponentTrigger("CustomHook")] // Generates CustomHook
///     public partial string Name { get; set; }
/// }
///
/// // Generated in TestModelComponent:
/// protected virtual void OnNotInBatchChanged() { }
/// protected virtual Task OnNotInBatchChangedAsync(CancellationToken ct)
///     => Task.CompletedTask;
///
/// // Override in .razor file:
/// @inherits TestModelComponent
/// @code {
///     protected override void OnNotInBatchChanged()
///     {
///         Console.WriteLine("Value changed!");
///     }
/// }
/// </code>
/// <para><b>Example - Cross-Model Trigger Propagation:</b></para>
/// <code>
/// // SettingsModel with trigger
/// [ObservableComponent]
/// public partial class SettingsModel : ObservableModel
/// {
///     [ObservableComponentTrigger]
///     public partial bool IsDay { get; set; }
/// }
///
/// // WeatherModel references SettingsModel
/// [ObservableComponent(includeReferencedTriggers: true)] // default
/// public partial class WeatherModel : ObservableModel
/// {
///     public partial WeatherModel(SettingsModel settings);
/// }
///
/// // Generated in WeatherModelComponent:
/// // - OnWeatherSettingsIsDayChanged() is automatically generated
/// // - OnWeatherSettingsIsDayChangedAsync(CancellationToken ct) is automatically generated
///
/// // Use in .razor file:
/// @inherits WeatherModelComponent
/// @code {
///     protected override void OnWeatherSettingsIsDayChanged()
///     {
///         // React to Settings.IsDay changes from weather component
///     }
/// }
/// </code>
/// <para><b>Generated Hook Methods:</b></para>
/// <list type="bullet">
/// <item>Sync: <c>protected virtual void On{Property}Changed()</c></item>
/// <item>Async: <c>protected virtual Task On{Property}ChangedAsync(CancellationToken ct)</c></item>
/// </list>
/// <para><b>Cross-Model Trigger Naming:</b></para>
/// <para>When a trigger is propagated to a referencing model's component, the hook method name follows:</para>
/// <para><c>On{CurrentModel}{ReferencedProperty}{TriggerProperty}Changed[Async]</c></para>
/// <para><b>Requirements for Cross-Model Triggers:</b></para>
/// <list type="bullet">
/// <item>Both models must be in the same assembly</item>
/// <item>Referencing model must have [ObservableComponent(includeReferencedTriggers: true)]</item>
/// <item>Referenced model must be injected via partial constructor parameter</item>
/// </list>
/// <para><b>Warning:</b> Avoid modifying the same property within its hook to prevent update loops.</para>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableComponentTriggerAttribute(ComponentTriggerType type = ComponentTriggerType.RenderAndHook, string? hookMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
