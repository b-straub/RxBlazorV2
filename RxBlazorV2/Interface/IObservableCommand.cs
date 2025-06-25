namespace RxBlazorV2.Interface;

public interface IObservableCommandBase
{
    public bool CanExecute { get; }
    
    public Exception? Error { get; }
    
    public void ResetError();
}

public interface IObservableCommandAsyncBase : IObservableCommandBase
{
    public bool Executing { get; }
}

public interface IObservableCommand : IObservableCommandBase
{
    public void Execute();
}

public interface IObservableCommand<in T> : IObservableCommandBase
{
    public void Execute(T parameter);
}

public interface IObservableCommandAsync : IObservableCommandAsyncBase
{
    public Task ExecuteAsync();

    public void Cancel();
}

public interface IObservableCommandAsync<in T> : IObservableCommandAsyncBase
{
    public Task ExecuteAsync(T parameter);
    
    public void Cancel();
}