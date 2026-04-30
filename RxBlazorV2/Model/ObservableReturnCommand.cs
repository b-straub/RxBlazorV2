using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

/// <summary>
/// Abstract base class for synchronous observable commands that return a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The return type of the command.</typeparam>
public abstract class ObservableCommandR<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandR<T>
{
    /// <summary>
    /// Executes the command and returns the result.
    /// </summary>
    /// <returns>The result of the command execution, or default if execution fails.</returns>
    public abstract T? Execute();
}

/// <summary>
/// Concrete synchronous observable command that returns a value of type <typeparamref name="T"/>, backed by a delegate.
/// </summary>
/// <typeparam name="T">The return type of the command.</typeparam>
public class ObservableCommandRFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T?> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandR<T>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Abstract base class for synchronous observable commands that accept a parameter of type <typeparamref name="T1"/> and return a value of type <typeparamref name="T2"/>.
/// </summary>
/// <typeparam name="T1">The parameter type of the command.</typeparam>
/// <typeparam name="T2">The return type of the command.</typeparam>
public abstract class ObservableCommandR<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandR<T1, T2>
{
    /// <summary>
    /// Executes the command with the specified parameter and returns the result.
    /// </summary>
    /// <param name="parameter">The input parameter for the command.</param>
    /// <returns>The result of the command execution, or default if execution fails.</returns>
    public abstract T2? Execute(T1 parameter);
}

/// <summary>
/// Concrete synchronous observable command that accepts a parameter of type <typeparamref name="T1"/> and returns a value of type <typeparamref name="T2"/>, backed by a delegate.
/// </summary>
/// <typeparam name="T1">The parameter type of the command.</typeparam>
/// <typeparam name="T2">The return type of the command.</typeparam>
public class ObservableCommandRFactory<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T1, T2?> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandR<T1, T2>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
