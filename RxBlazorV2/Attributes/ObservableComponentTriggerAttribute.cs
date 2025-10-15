#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Generates virtual hook methods in the component class for property change events.
/// Use with [ObservableComponent] attribute to create component-level property change handlers.
/// Multiple triggers can be applied to the same property.
/// </summary>
/// <param name="hookMethodName">
/// Optional. The name of the hook method to generate.
/// If not specified, defaults to On{PropertyName}Changed.
/// Use <see langword="nameof"/> for compile-time safety when specifying.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties in models decorated with [ObservableComponent].</para>
/// <para><b>Example:</b></para>
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
/// <para><b>Generated Hook Methods:</b></para>
/// <list type="bullet">
/// <item>Sync: <c>protected virtual void On{Property}Changed()</c></item>
/// <item>Async: <c>protected virtual Task On{Property}ChangedAsync(CancellationToken ct)</c></item>
/// </list>
/// <para><b>Warning:</b> Avoid modifying the same property within its hook to prevent update loops.</para>
/// </remarks>

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableComponentTriggerAttribute(string? hookMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
