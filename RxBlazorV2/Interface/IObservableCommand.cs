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

public interface IObservableCommandBase
{
    public bool CanExecute { get; }

    public Exception? Error { get; }

    public void ResetError();
}

public interface IObservableCommandAsyncBase : IObservableCommandBase
{
    public bool Executing { get; }

    /// <summary>
    /// Reason for the last cancellation. Reset to NONE when command starts.
    /// </summary>
    public CancellationReason LastCancellationReason { get; }

    public void Cancel();
}

public interface IObservableCommand : IObservableCommandBase
{
    public void Execute();
}

public interface IObservableCommand<in T> : IObservableCommandBase
{
    public void Execute(T parameter);
}

public interface IObservableCommandR<out T> : IObservableCommandBase
{
    public T? Execute();
}

public interface IObservableCommandR<in T1, out T2> : IObservableCommandBase
{
    public T2? Execute(T1 parameter);
}

public interface IObservableCommandAsync : IObservableCommandAsyncBase
{
    public Task ExecuteAsync();
}

public interface IObservableCommandAsync<in T> : IObservableCommandAsyncBase
{
    public Task ExecuteAsync(T parameter);
}

public interface IObservableCommandRAsync<T> : IObservableCommandAsyncBase
{
    public Task<T?> ExecuteAsync();
}

public interface IObservableCommandRAsync<in T1, T2> : IObservableCommandAsyncBase
{
    public Task<T2?> ExecuteAsync(T1 parameter);
}