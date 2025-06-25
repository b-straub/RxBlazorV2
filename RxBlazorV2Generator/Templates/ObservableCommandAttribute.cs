#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Marks a property as an observable command that will be automatically implemented by the source generator.
/// The property must be partial and have a type that implements <see cref="IObservableCommand"/> or <see cref="IObservableCommandAsync"/>.
/// </summary>
/// <param name="executionMethodName">
/// The name of the private method that will be executed when the command is invoked.
/// Use <see langword="nameof"/> for compile-time safety (e.g., <c>nameof(MyExecuteMethod)</c>).
/// </param>
/// <param name="canExecuteMethodName">
/// Optional. The name of the private method that determines if the command can be executed.
/// Must return <see langword="bool"/>. Use <see langword="nameof"/> for compile-time safety (e.g., <c>nameof(MyCanExecuteMethod)</c>).
/// If not provided, the command will always be executable.
/// </param>
/// <remarks>
/// <para>Supported command types and their corresponding method signatures:</para>
/// <list type="bullet">
/// <item>
/// <term><see cref="IObservableCommand"/></term>
/// <description><see langword="void"/> ExecuteMethod() - Synchronous command without parameters</description>
/// </item>
/// <item>
/// <term><see cref="IObservableCommandAsync"/></term>
/// <description><see cref="Task"/> ExecuteMethod() - Asynchronous command without parameters</description>
/// </item>
/// <item>
/// <term><see cref="IObservableCommand{T}"/></term>
/// <description><see langword="void"/> ExecuteMethod(T parameter) - Synchronous command with parameter</description>
/// </item>
/// <item>
/// <term><see cref="IObservableCommandAsync{T}"/></term>
/// <description><see cref="Task"/> ExecuteMethod(T parameter) - Asynchronous command with parameter</description>
/// </item>
/// <item>
/// <term><see cref="IObservableCommandAsync{T}"/> with cancellation</term>
/// <description><see cref="Task"/> ExecuteMethod(T parameter, <see cref="CancellationToken"/> token) - Asynchronous command with parameter and cancellation support</description>
/// </item>
/// </list>
/// <para>The source generator automatically detects which properties are accessed in the execute and canExecute methods to set up reactive change notifications.</para>
/// </remarks>
/// <example>
/// <code>
/// public partial class MyModel : ObservableModel
/// {
///     public partial string Name { get; set; }
///     public partial int Count { get; set; }
///     
///     [ObservableCommand(nameof(IncrementCount), nameof(CanIncrement))]
///     public partial IObservableCommand IncrementCommand { get; }
///     
///     [ObservableCommand(nameof(SetNameAsync))]
///     public partial IObservableCommandAsync&lt;string&gt; SetNameCommand { get; }
///     
///     private void IncrementCount()
///     {
///         Count++; // Generator detects Count property usage
///     }
///     
///     private bool CanIncrement()
///     {
///         return Count &lt; 100; // Generator detects Count property usage
///     }
///     
///     private async Task SetNameAsync(string newName)
///     {
///         await Task.Delay(100);
///         Name = newName; // Generator detects Name property usage
///     }
/// }
/// </code>
/// </example>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ObservableCommandAttribute(string executionMethodName, string? canExecuteMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113