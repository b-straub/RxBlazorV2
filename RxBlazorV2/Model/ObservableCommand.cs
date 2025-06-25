using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public class ObservableCommandBase(ObservableModel model, string[] observedProperties)
    : Observable<string[]>, IObservableCommandBase
{
    public virtual bool CanExecute => true;
    public Exception? Error { get; private set; }
    
    public void ResetError()
    {
        Error = null;
    }

    protected void SetError(Exception? exception = null)
    {
        Error = exception;
    }
    
    protected override IDisposable SubscribeCore(Observer<string[]> observer)
    {
        return model.Observable
            .Where(p => p.Length == 0 || observedProperties.Intersect(p).Any())
            .Subscribe(observer);
    }
}

public class ObservableCommandAsyncBase(ObservableModel model, string[] observedProperties)
    : ObservableCommandBase(model, observedProperties), IObservableCommandAsyncBase
{
    public virtual bool Executing { get; protected set; }
    
    protected CancellationToken CancellationToken { get; private set; }
    private CancellationTokenSource? _cancellationTokenSource;
    
    protected void ResetCancellationToken()
    {
        if (_cancellationTokenSource is not null && !_cancellationTokenSource.TryReset())
        {
            _cancellationTokenSource = new();
            CancellationToken = _cancellationTokenSource.Token;
        }
        else
        {
            _cancellationTokenSource = new();
        }
    }

    public virtual void Cancel()
    {
        ArgumentNullException.ThrowIfNull(_cancellationTokenSource);
        _cancellationTokenSource.Cancel();
    }
}

public abstract class ObservableCommand(ObservableModel model, string[] observedProperties)
    : ObservableCommandBase(model, observedProperties), IObservableCommand
{
    public abstract void Execute();
}

public class ObservableCommandFactory(
    ObservableModel model,
    string[] observedProperties,
    Action execute,
    Func<bool>? canExecute = null) :
    ObservableCommand(model, observedProperties)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override void Execute()
    {
        SetError();
        try
        {
            execute();
        }
        catch (Exception e)
        {
            SetError(e);
        }
        
        _model.PropertyChangedSubject.OnNext(_observedProperties);
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public abstract class ObservableCommand<T>(ObservableModel model, string[] observedProperties)
    : ObservableCommandBase(model, observedProperties), IObservableCommand<T>
{
    public abstract void Execute(T parameter);
}

public class ObservableCommandFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    Action<T> execute,
    Func<bool>? canExecute = null) :
    ObservableCommand<T>(model, observedProperties)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override void Execute(T parameter)
    {
        SetError();
        try
        {
            execute(parameter);
        }
        catch (Exception e)
        {
            SetError(e);
        }
        
        _model.PropertyChangedSubject.OnNext(_observedProperties);
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public abstract class ObservableCommandAsync(ObservableModel model, string[] observedProperties)
    : ObservableCommandAsyncBase(model, observedProperties), IObservableCommandAsync
{
    public abstract Task ExecuteAsync();
}

public class ObservableCommandAsyncFactory(
    ObservableModel model,
    string[] observedProperties,
    Func<Task> execute,
    Func<bool>? canExecute = null) :
    ObservableCommandAsync(model, observedProperties)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override async Task ExecuteAsync()
    {
        Executing = true;
        _model.PropertyChangedSubject.OnNext(_observedProperties);
        
        SetError();
        try
        {
            await execute();
        }
        catch (Exception e)
        {
            SetError(e);
        }
        
        Executing = false;
        _model.PropertyChangedSubject.OnNext(_observedProperties);
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public class ObservableCommandAsyncCancelableFactory(
    ObservableModel model,
    string[] observedProperties,
    Func<CancellationToken, Task> execute,
    Func<bool>? canExecute = null) :
    ObservableCommandAsync(model, observedProperties)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;
 
    public override async Task ExecuteAsync()
    {
        ResetCancellationToken();
        Executing = true;
        _model.PropertyChangedSubject.OnNext(_observedProperties);

        SetError();
        try
        {
            await execute(CancellationToken);
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(TaskCanceledException))
            {
                SetError(e);
            }
        }

        Executing = false;
        _model.PropertyChangedSubject.OnNext(_observedProperties);
    }
    
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public abstract class ObservableCommandAsync<T>(ObservableModel model, string[] observedProperties)
    : ObservableCommandAsyncBase(model, observedProperties), IObservableCommandAsync<T>
{
    public abstract Task ExecuteAsync(T parameter);
}

public class ObservableCommandAsyncFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    Func<T, Task> execute,
    Func<bool>? canExecute = null) :
    ObservableCommandAsync<T>(model, observedProperties)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override async Task ExecuteAsync(T parameter)
    {
        Executing = true;
        _model.PropertyChangedSubject.OnNext(_observedProperties);
        
        SetError();
        try
        {
            await execute(parameter);
        }
        catch (Exception e)
        {
            SetError(e);
        }
        
        _model.PropertyChangedSubject.OnNext(_observedProperties);
        Executing = false;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public class ObservableCommandAsyncCancelableFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    Func<T, CancellationToken, Task> execute,
    Func<bool>? canExecute = null) :
    ObservableCommandAsync<T>(model, observedProperties)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;
   
    public override async Task ExecuteAsync(T parameter)
    {
        ResetCancellationToken();
        Executing = true;
        _model.PropertyChangedSubject.OnNext(_observedProperties);
        
        SetError();
        try
        {
            await execute(parameter, CancellationToken);
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(TaskCanceledException))
            {
                SetError(e);
            }
        }
        
        _model.PropertyChangedSubject.OnNext(_observedProperties);
        Executing = false;
    }
    
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}