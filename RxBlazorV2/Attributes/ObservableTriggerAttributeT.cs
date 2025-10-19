#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RxBlazorV2.Model;

/// <summary>
/// Automatically executes a parametrized method when the decorated property changes.
/// This is a shortcut alternative to creating explicit parametrized command properties with triggers.
/// </summary>
/// <typeparam name="T">
/// The parameter type passed to the execution method.
/// </typeparam>
/// <param name="executionMethodName">
/// The name of the private method to execute when the property changes.
/// The method must accept a parameter of type T.
/// Use <see langword="nameof"/> for compile-time safety.
/// </param>
/// <param name="parameter">
/// The value to pass to the execution method when triggered.
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute.
/// Must return <see langword="bool"/>.
/// </param>
/// <remarks>
/// <para><b>Usage:</b> Apply to partial properties to automatically execute parametrized methods on property changes.</para>
/// <para><b>Example:</b></para>
/// <code>
/// [ObservableTrigger&lt;string&gt;(nameof(LogChange), "PropertyChanged")]
/// public partial int Count { get; set; }
///
/// private void LogChange(string message)
/// {
///     Console.WriteLine($"{message}: {Count}");
/// }
/// </code>
/// <para><b>Warning:</b> Avoid circular triggers where the method modifies the same property it listens to.</para>
/// </remarks>

#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableTriggerAttribute<T>(string executionMethodName, T parameter, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113
