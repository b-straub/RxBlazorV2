namespace RxBlazorV2.Interface;

/// <summary>
/// Reason for command cancellation.
/// </summary>
public enum CancellationReason
{
    /// <summary>
    /// No cancellation occurred.
    /// </summary>
    NONE,

    /// <summary>
    /// Cancelled explicitly via Cancel() method.
    /// </summary>
    EXPLICIT,

    /// <summary>
    /// Cancelled implicitly by Switch operation (new trigger fired).
    /// </summary>
    SWITCH
}

/// <summary>
/// Base interface for all observable commands providing execution guard and error tracking.
/// </summary>
public interface IObservableCommandBase
{
    /// <summary>
    /// Gets whether the command can currently execute.
    /// </summary>
    public bool CanExecute { get; }

    /// <summary>
    /// Gets the last exception thrown during command execution, or null if none.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Gets the user-facing message produced by the per-command error formatter (configured via the
    /// third argument of <c>[ObservableCommand]</c>), or <c>null</c> when no error is recorded or
    /// when a <c>StatusBaseModel</c> is configured (the formatted message flows there instead).
    /// Falls back to <see cref="Exception.Message"/> when no formatter is configured.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Clears the current error state.
    /// </summary>
    public void ResetError();
}

/// <summary>
/// Base interface for async observable commands with execution tracking and cancellation support.
/// </summary>
public interface IObservableCommandAsyncBase : IObservableCommandBase
{
    /// <summary>
    /// Gets whether the command is currently executing.
    /// </summary>
    public bool Executing { get; }

    /// <summary>
    /// Reason for the last cancellation. Reset to NONE when command starts.
    /// </summary>
    public CancellationReason LastCancellationReason { get; }

    /// <summary>
    /// Cancels the currently executing command.
    /// </summary>
    public void Cancel();
}

/// <summary>
/// Synchronous observable command without parameters.
/// </summary>
public interface IObservableCommand : IObservableCommandBase
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    public void Execute();
}

/// <summary>
/// Synchronous observable command accepting a parameter of type <typeparamref name="T"/>.
/// </summary>
public interface IObservableCommand<in T> : IObservableCommandBase
{
    /// <summary>
    /// Executes the command with the specified parameter.
    /// </summary>
    public void Execute(T parameter);
}

/// <summary>
/// Synchronous observable command returning a result of type <typeparamref name="T"/>.
/// </summary>
public interface IObservableCommandR<out T> : IObservableCommandBase
{
    /// <summary>
    /// Executes the command and returns the result.
    /// </summary>
    public T? Execute();
}

/// <summary>
/// Synchronous observable command accepting a parameter of type <typeparamref name="T1"/> and returning a result of type <typeparamref name="T2"/>.
/// </summary>
public interface IObservableCommandR<in T1, out T2> : IObservableCommandBase
{
    /// <summary>
    /// Executes the command with the specified parameter and returns the result.
    /// </summary>
    public T2? Execute(T1 parameter);
}

/// <summary>
/// Asynchronous observable command without parameters.
/// </summary>
public interface IObservableCommandAsync : IObservableCommandAsyncBase
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    public Task ExecuteAsync();
}

/// <summary>
/// Asynchronous observable command accepting a parameter of type <typeparamref name="T"/>.
/// </summary>
public interface IObservableCommandAsync<in T> : IObservableCommandAsyncBase
{
    /// <summary>
    /// Executes the command asynchronously with the specified parameter.
    /// </summary>
    public Task ExecuteAsync(T parameter);
}

/// <summary>
/// Asynchronous observable command returning a result of type <typeparamref name="T"/>.
/// </summary>
public interface IObservableCommandRAsync<T> : IObservableCommandAsyncBase
{
    /// <summary>
    /// Executes the command asynchronously and returns the result.
    /// </summary>
    public Task<T?> ExecuteAsync();
}

/// <summary>
/// Asynchronous observable command accepting a parameter of type <typeparamref name="T1"/> and returning a result of type <typeparamref name="T2"/>.
/// </summary>
public interface IObservableCommandRAsync<in T1, T2> : IObservableCommandAsyncBase
{
    /// <summary>
    /// Executes the command asynchronously with the specified parameter and returns the result.
    /// </summary>
    public Task<T2?> ExecuteAsync(T1 parameter);
}