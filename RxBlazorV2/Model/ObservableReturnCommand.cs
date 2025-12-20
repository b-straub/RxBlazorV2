using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public abstract class ObservableCommandR<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandR<T>
{
    public abstract T? Execute();
}

public class ObservableCommandRFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T?> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null) :
    ObservableCommandR<T>(model, observedProperties, commandName, methodName, statusModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override T? Execute()
    {
        T? result = default;

        SetError();
        try
        {
            result = execute();
        }
        catch (Exception e)
        {
            SetError(e);
        }

        _model.StateHasChanged(_observedProperties);
        return result;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public abstract class ObservableCommandR<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandR<T1, T2>
{
    public abstract T2? Execute(T1 parameter);
}

public class ObservableCommandRFactory<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T1, T2?> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null) :
    ObservableCommandR<T1, T2>(model, observedProperties, commandName, methodName, statusModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override T2? Execute(T1 parameter)
    {
        T2? result = default;

        SetError();
        try
        {
            result = execute(parameter);
        }
        catch (Exception e)
        {
            SetError(e);
        }

        _model.StateHasChanged(_observedProperties);
        return result;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
