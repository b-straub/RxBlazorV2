using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

/// <summary>
/// Base class for all observable command implementations, extending <see cref="Observable{T}"/> with property change filtering.
/// </summary>
/// <param name="model">The owning <see cref="ObservableModel"/> whose property changes are observed.</param>
/// <param name="observedProperties">Property names that trigger re-evaluation of this command.</param>
/// <param name="commandName">Display name of the command, used for error reporting.</param>
/// <param name="methodName">Name of the backing method, used for error reporting.</param>
/// <param name="statusModel">Optional <see cref="StatusBaseModel"/> to delegate error handling to.</param>
public class ObservableCommandBase(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : Observable<string[]>, IObservableCommandBase
{
    /// <summary>
    /// Gets a value indicating whether the command can currently execute. Returns <c>true</c> by default.
    /// </summary>
    public virtual bool CanExecute => true;

    /// <summary>
    /// Gets the most recent exception thrown during command execution, or <c>null</c> if no error occurred.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Clears the current <see cref="Error"/>.
    /// </summary>
    public void ResetError()
    {
        Error = null;
    }

    /// <summary>
    /// Sets or clears the current error. When a <see cref="StatusBaseModel"/> is configured, delegates the error to it instead.
    /// </summary>
    /// <param name="exception">The exception to record, or <c>null</c> to clear the error.</param>
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

    /// <summary>
    /// Subscribes the observer to the owning model's property change notifications, filtered to the observed properties.
    /// </summary>
    protected override IDisposable SubscribeCore(Observer<string[]> observer)
    {
        return model.Observable
            .Where(p => p.Length == 0 || observedProperties.Intersect(p).Any())
            .Subscribe(observer);
    }
}

/// <summary>
/// Abstract base class for synchronous observable commands.
/// </summary>
/// <param name="model">The owning <see cref="ObservableModel"/> whose property changes are observed.</param>
/// <param name="observedProperties">Property names that trigger re-evaluation of this command.</param>
/// <param name="commandName">Display name of the command, used for error reporting.</param>
/// <param name="methodName">Name of the backing method, used for error reporting.</param>
/// <param name="statusModel">Optional <see cref="StatusBaseModel"/> to delegate error handling to.</param>
public abstract class ObservableCommand(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommand
{
    /// <summary>
    /// Executes the command synchronously.
    /// </summary>
    public abstract void Execute();
}

/// <summary>
/// Concrete synchronous command implementation that invokes an <see cref="Action"/> delegate.
/// </summary>
/// <param name="model">The owning <see cref="ObservableModel"/> whose property changes are observed.</param>
/// <param name="observedProperties">Property names that trigger re-evaluation and are notified after execution.</param>
/// <param name="commandName">Display name of the command, used for error reporting.</param>
/// <param name="methodName">Name of the backing method, used for error reporting.</param>
/// <param name="execute">The delegate to invoke when the command executes.</param>
/// <param name="canExecute">Optional guard function that determines whether the command can execute.</param>
/// <param name="statusModel">Optional <see cref="StatusBaseModel"/> to delegate error handling to.</param>
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

    /// <summary>
    /// Executes the command by invoking the backing delegate and notifying observed properties.
    /// </summary>
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

    /// <summary>
    /// Gets a value indicating whether the command can currently execute, as determined by the guard function.
    /// </summary>
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Abstract base class for synchronous parametrized observable commands.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
/// <param name="model">The owning <see cref="ObservableModel"/> whose property changes are observed.</param>
/// <param name="observedProperties">Property names that trigger re-evaluation of this command.</param>
/// <param name="commandName">Display name of the command, used for error reporting.</param>
/// <param name="methodName">Name of the backing method, used for error reporting.</param>
/// <param name="statusModel">Optional <see cref="StatusBaseModel"/> to delegate error handling to.</param>
public abstract class ObservableCommand<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommand<T>
{
    /// <summary>
    /// Executes the command synchronously with the given parameter.
    /// </summary>
    /// <param name="parameter">The parameter to pass to the command.</param>
    public abstract void Execute(T parameter);
}

/// <summary>
/// Concrete synchronous parametrized command implementation that invokes an <see cref="Action{T}"/> delegate.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
/// <param name="model">The owning <see cref="ObservableModel"/> whose property changes are observed.</param>
/// <param name="observedProperties">Property names that trigger re-evaluation and are notified after execution.</param>
/// <param name="commandName">Display name of the command, used for error reporting.</param>
/// <param name="methodName">Name of the backing method, used for error reporting.</param>
/// <param name="execute">The delegate to invoke when the command executes.</param>
/// <param name="canExecute">Optional guard function that determines whether the command can execute.</param>
/// <param name="statusModel">Optional <see cref="StatusBaseModel"/> to delegate error handling to.</param>
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

    /// <summary>
    /// Executes the command by invoking the backing delegate with the given parameter and notifying observed properties.
    /// </summary>
    /// <param name="parameter">The parameter to pass to the command.</param>
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

    /// <summary>
    /// Gets a value indicating whether the command can currently execute, as determined by the guard function.
    /// </summary>
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
