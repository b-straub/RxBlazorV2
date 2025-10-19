#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RxBlazorV2.Model;

/// <summary>
/// Automatically executes a method when the decorated property changes.
/// This is a shortcut alternative to creating explicit command properties with triggers.
/// Multiple triggers can be applied to the same property.
/// </summary>
/// <param name="executionMethodName">
/// The name of the private method to execute when the property changes.
/// Supports sync/async methods with or without parameters.
/// Use <see langword="nameof"/> for compile-time safety.
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute.
/// Must return <see langword="bool"/>.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties to automatically execute methods on property changes.</para>
/// <para><b>Example:</b></para>
/// <code>
/// [ObservableTrigger(nameof(ValidateInput))]
/// [ObservableTrigger(nameof(SaveAsync), nameof(CanSave))]
/// public partial string Input { get; set; }
///
/// private void ValidateInput() { /* validation logic */ }
/// private async Task SaveAsync() { /* save logic */ }
/// private bool CanSave() => !string.IsNullOrEmpty(Input);
/// </code>
/// <para><b>Warning:</b> Avoid circular triggers where the method modifies the same property it listens to.</para>
/// <para>See <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG012.md">RXBG012</see> for circular trigger troubleshooting.</para>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableTriggerAttribute(string executionMethodName, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
