#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Automatically triggers a parametrized command when a property changes, passing a predefined parameter value.
/// </summary>
/// <typeparam name="T">
/// The parameter type. Must match the command's generic type parameter exactly.
/// </typeparam>
/// <param name="triggerProperty">
/// The property that triggers command execution when it changes.
/// </param>
/// <param name="parameter">
/// The value to pass to the command when triggered.
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute.
/// </param>
/// <remarks>
/// <para>See <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG011.md">RXBG011</see> for type parameter requirements.</para>
/// </remarks>
#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableCommandTriggerAttribute<T>(string triggerProperty, T parameter, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113