using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public class ObservableCommandBase(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
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
        if (exception is not null && statusModel is not null)
        {
            // Delegate error to status model with command source info
            statusModel.HandleError(exception, commandName, methodName);
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

public abstract class ObservableCommand(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommand
{
    public abstract void Execute();
}

public class ObservableCommandFactory(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Action execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null) :
    ObservableCommand(model, observedProperties, commandName, methodName, statusModel)
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

public abstract class ObservableCommand<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommand<T>
{
    public abstract void Execute(T parameter);
}

public class ObservableCommandFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Action<T> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null) :
    ObservableCommand<T>(model, observedProperties, commandName, methodName, statusModel)
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
