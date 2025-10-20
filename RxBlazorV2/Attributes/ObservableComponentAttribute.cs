#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Generates a complete Blazor component class that inherits from ObservableComponent&lt;T&gt;.
/// The generated component automatically subscribes to model property changes based on batches
/// and can optionally generate hooks for triggers in referenced ObservableModels.
/// </summary>
/// <param name="includeReferencedTriggers">
/// Optional. When true (default), generates hook methods for [ObservableComponentTrigger] properties
/// in referenced ObservableModels (via partial constructor parameters). Referenced models must be in
/// the same assembly for this feature to work. Set to false to disable cross-model trigger generation.
/// </param>
/// <param name="componentName">
/// Optional. The name of the generated component class.
/// If not specified, defaults to {ModelName}Component.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to ObservableModel classes to generate corresponding component classes.</para>
/// <para><b>Example - Basic Usage:</b></para>
/// <code>
/// [ObservableComponent] // Generates TestModelComponent
/// public partial class TestModel : ObservableModel
/// {
///     [ObservableBatch("common")]
///     public partial int Count { get; set; }
/// }
///
/// // In .razor file:
/// @inherits TestModelComponent
/// &lt;div&gt;@Model.Count&lt;/div&gt;
/// </code>
/// <para><b>Example - Referenced Model Triggers:</b></para>
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
/// [ObservableComponent] // includeReferencedTriggers defaults to true
/// public partial class WeatherModel : ObservableModel
/// {
///     public partial WeatherModel(SettingsModel settings); // Creates Settings property
/// }
///
/// // Generated in WeatherModelComponent:
/// // - OnWeatherSettingsIsDayChanged() hook method
/// // - OnWeatherSettingsIsDayChangedAsync(CancellationToken ct) hook method
/// </code>
/// <para><b>Generated Component Features:</b></para>
/// <list type="bullet">
/// <item>Inherits from ObservableComponent&lt;T&gt; where T is your model</item>
/// <item>Automatic subscription management via CompositeDisposable</item>
/// <item>Batched property change notifications based on [ObservableComponentTrigger] attributes</item>
/// <item>Lifecycle hooks: InitializeGeneratedCode, InitializeGeneratedCodeAsync</item>
/// <item>Virtual hook methods for local [ObservableComponentTrigger] properties</item>
/// <item>Virtual hook methods for referenced model triggers (when includeReferencedTriggers=true)</item>
/// </list>
/// <para><b>Referenced Trigger Naming Convention:</b></para>
/// <para>Hook methods for referenced model triggers follow the pattern:</para>
/// <para><c>On{CurrentModel}{ReferencedProperty}{TriggerProperty}Changed[Async]</c></para>
/// <para>Example: <c>OnWeatherSettingsIsDayChanged</c> where:</para>
/// <list type="bullet">
/// <item>Weather = current model (WeatherModel with "Model" suffix stripped)</item>
/// <item>Settings = referenced property name (from parameter 'settings')</item>
/// <item>IsDay = trigger property on referenced model</item>
/// </list>
/// <para><b>Assembly Requirement:</b></para>
/// <para>Referenced models with triggers must be in the same assembly. If a referenced model
/// is in a different assembly, either move it to the same assembly or set includeReferencedTriggers=false.</para>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class)]
public class ObservableComponentAttribute(bool includeReferencedTriggers = true, string? componentName = null)
    : Attribute
{
}
#pragma warning restore CS9113
