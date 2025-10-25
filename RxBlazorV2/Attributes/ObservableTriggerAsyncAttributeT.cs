#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Automatically executes a parametrized async method when the decorated property changes.
/// This is a shortcut alternative to creating explicit parametrized async command properties with triggers.
/// </summary>
/// <typeparam name="T">
/// The parameter type passed to the async execution method.
/// </typeparam>
/// <param name="executionMethodName">
/// The name of the private async method to execute when the property changes.
/// The method must accept a parameter of type T and return Task or ValueTask.
/// May optionally accept a second CancellationToken parameter.
/// Use <see langword="nameof"/> for compile-time safety.
/// </param>
/// <param name="parameter">
/// The value to pass to the async execution method when triggered.
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute.
/// Must return <see langword="bool"/>.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties to automatically execute parametrized async methods on property changes.</para>
/// <para><b>Example:</b></para>
/// <code>
/// [ObservableTriggerAsync&lt;string&gt;(nameof(LogChangeAsync), "PropertyChanged")]
/// [ObservableTriggerAsync&lt;int&gt;(nameof(ValidateRangeAsync), 100, nameof(CanValidate))]
/// public partial int Count { get; set; }
///
/// private async Task LogChangeAsync(string message)
/// {
///     await _logger.LogAsync($"{message}: {Count}");
/// }
///
/// private async Task ValidateRangeAsync(int maxValue, CancellationToken ct)
/// {
///     if (Count > maxValue)
///     {
///         await _validator.ReportViolationAsync(Count, maxValue, ct);
///     }
/// }
///
/// private bool CanValidate() => Count > 0;
/// </code>
/// <para><b>Async Method Signatures:</b></para>
/// <list type="bullet">
/// <item><c>Task Method(T parameter)</c> - Basic async execution with parameter</item>
/// <item><c>Task Method(T parameter, CancellationToken ct)</c> - With cancellation support</item>
/// <item><c>ValueTask Method(T parameter)</c> - High-performance async</item>
/// <item><c>ValueTask Method(T parameter, CancellationToken ct)</c> - High-performance with cancellation</item>
/// </list>
/// <para><b>Warning:</b> Avoid circular triggers where the method modifies the same property it listens to.</para>
/// <para><b>See Also:</b></para>
/// <list type="bullet">
/// <item><see cref="ObservableTriggerAttribute{T}"/> - For sync parametrized triggers</item>
/// <item><see cref="ObservableTriggerAsyncAttribute"/> - For non-parametrized async triggers</item>
/// <item><see cref="ObservableCallbackTriggerAsyncAttribute"/> - For external service subscriptions</item>
/// </list>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableTriggerAsyncAttribute<T>(string executionMethodName, T parameter, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
