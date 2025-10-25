#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Automatically executes an async method when the decorated property changes.
/// This is a shortcut alternative to creating explicit async command properties with triggers.
/// Multiple triggers can be applied to the same property.
/// </summary>
/// <param name="executionMethodName">
/// The name of the private async method to execute when the property changes.
/// Method must return Task or ValueTask and may optionally accept a CancellationToken parameter.
/// Use <see langword="nameof"/> for compile-time safety.
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute.
/// Must return <see langword="bool"/>.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties to automatically execute async methods on property changes.</para>
/// <para><b>Example:</b></para>
/// <code>
/// [ObservableTriggerAsync(nameof(SaveAsync))]
/// [ObservableTriggerAsync(nameof(ValidateAsync), nameof(CanValidate))]
/// public partial string Input { get; set; }
///
/// private async Task SaveAsync()
/// {
///     await _repository.SaveAsync(Input);
/// }
///
/// private async Task ValidateAsync(CancellationToken ct)
/// {
///     await _validator.ValidateAsync(Input, ct);
/// }
///
/// private bool CanValidate() => !string.IsNullOrEmpty(Input);
/// </code>
/// <para><b>Async Method Signatures:</b></para>
/// <list type="bullet">
/// <item><c>Task Method()</c> - Basic async execution</item>
/// <item><c>Task Method(CancellationToken ct)</c> - With cancellation support</item>
/// <item><c>ValueTask Method()</c> - High-performance async</item>
/// <item><c>ValueTask Method(CancellationToken ct)</c> - High-performance with cancellation</item>
/// </list>
/// <para><b>Warning:</b> Avoid circular triggers where the method modifies the same property it listens to.</para>
/// <para><b>See Also:</b></para>
/// <list type="bullet">
/// <item><see cref="ObservableTriggerAttribute"/> - For sync method triggers</item>
/// <item><see cref="ObservableTriggerAsyncAttribute{T}"/> - For parametrized async triggers</item>
/// <item><see cref="ObservableCallbackTriggerAsyncAttribute"/> - For external service subscriptions</item>
/// </list>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableTriggerAsyncAttribute(string executionMethodName, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
