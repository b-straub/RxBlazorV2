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

        _model.StateHasChanged(_observedProperties);
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

        _model.StateHasChanged(_observedProperties);
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
