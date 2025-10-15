#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Marks a partial property as an observable command that will be automatically implemented by the source generator.
/// Supports sync/async commands with or without parameters.
/// </summary>
/// <param name="executionMethodName">
/// The name of the private method that executes when the command is invoked.
/// Use <see langword="nameof"/> for compile-time safety.
/// </param>
/// <param name="canExecuteMethodName">
/// Optional. The name of the private method that determines if the command can execute.
/// Must return <see langword="bool"/>. Use <see langword="nameof"/> for compile-time safety.
/// </param>
/// <remarks>
/// <para>The generator automatically detects property usage in methods to set up reactive change notifications.</para>
/// </remarks>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property)]
public class ObservableCommandAttribute(string executionMethodName, string? canExecuteMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113