using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public class ObservableCommandBase(ObservableModel model, string[] observedProperties, IErrorModel? errorModel = null)
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
        if (exception is not null && errorModel is not null)
        {
            // Delegate error to error model and reset command error
            errorModel.HandleError(exception);
            Error = null;
        }
        else
        {
            Error = exception;
        }
    }

    protected override IDisposable SubscribeCore(Observer<string[]> observer)
    {
        return model.Observable
            .Where(p => p.Length == 0 || observedProperties.Intersect(p).Any())
            .Subscribe(observer);
    }
}

public abstract class ObservableCommand(ObservableModel model, string[] observedProperties, IErrorModel? errorModel = null)
    : ObservableCommandBase(model, observedProperties, errorModel), IObservableCommand
{
    public abstract void Execute();
}

public class ObservableCommandFactory(
    ObservableModel model,
    string[] observedProperties,
    Action execute,
    Func<bool>? canExecute = null,
    IErrorModel? errorModel = null) :
    ObservableCommand(model, observedProperties, errorModel)
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

public abstract class ObservableCommand<T>(ObservableModel model, string[] observedProperties, IErrorModel? errorModel = null)
    : ObservableCommandBase(model, observedProperties, errorModel), IObservableCommand<T>
{
    public abstract void Execute(T parameter);
}

public class ObservableCommandFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    Action<T> execute,
    Func<bool>? canExecute = null,
    IErrorModel? errorModel = null) :
    ObservableCommand<T>(model, observedProperties, errorModel)
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
