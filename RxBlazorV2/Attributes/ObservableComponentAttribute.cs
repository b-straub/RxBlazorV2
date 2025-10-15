#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Generates a complete Blazor component class that inherits from ObservableComponent&lt;T&gt;.
/// The generated component automatically subscribes to model property changes based on batches.
/// </summary>
/// <param name="componentName">
/// Optional. The name of the generated component class.
/// If not specified, defaults to {ModelName}Component.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to ObservableModel classes to generate corresponding component classes.</para>
/// <para><b>Example:</b></para>
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
/// <para><b>Generated Component Features:</b></para>
/// <list type="bullet">
/// <item>Inherits from ObservableComponent&lt;T&gt; where T is your model</item>
/// <item>Automatic subscription management via CompositeDisposable</item>
/// <item>Batched property change notifications based on [ObservableBatch] attributes</item>
/// <item>Lifecycle hooks: InitializeGeneratedCode, InitializeGeneratedCodeAsync</item>
/// <item>Virtual hook methods for properties with [ObservableComponentTrigger]</item>
/// </list>
/// </remarks>

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ObservableComponentAttribute(string? componentName = null)
    : Attribute
{
}
#pragma warning restore CS9113
