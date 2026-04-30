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
/// <param name="errorFormatter">Optional formatter that maps an <see cref="Exception"/> to a user-facing
/// string. Invoked before delegating to <paramref name="statusModel"/> or populating <c>ErrorMessage</c>.</param>
public class ObservableCommandBase(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : Observable<string[]>, IObservableCommandBase
{
    /// <summary>
    /// Gets a value indicating whether the command can currently execute. Returns <c>true</c> by default.
    /// </summary>
    public virtual bool CanExecute => true;

    /// <summary>
    /// Gets the most recent exception thrown during command execution, or <c>null</c> if no error occurred.
    /// Always populated when the command body throws, regardless of whether a <see cref="StatusBaseModel"/>
    /// is configured — render this surface for per-command inline alerts.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Gets the user-facing message produced by the configured formatter, or <see cref="Exception.Message"/>
    /// when no formatter is configured. Populated whenever <see cref="Error"/> is, so consumers can bind a
    /// per-command alert to <c>Command.ErrorMessage</c> independently of any global status sink. The same
    /// formatted text is also forwarded to the configured <see cref="StatusBaseModel"/> when one is wired.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Clears the current <see cref="Error"/> and <see cref="ErrorMessage"/>.
    /// </summary>
    public void ResetError()
    {
        Error = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Sets or clears the current error. Always records both the raw exception (<see cref="Error"/>) and the
    /// formatted user-facing text (<see cref="ErrorMessage"/>) on the command, and additionally forwards the
    /// formatted text to a configured <see cref="StatusBaseModel"/>. The consumer chooses which surface to
    /// render — rendering both is a deliberate choice (e.g. inline alert + global status log).
    /// </summary>
    /// <param name="exception">The exception to record, or <c>null</c> to clear the error.</param>
    protected void SetError(Exception? exception = null)
    {
        if (exception is null)
        {
            Error = null;
            ErrorMessage = null;
            return;
        }

        var formatted = errorFormatter is not null
            ? errorFormatter(exception)
            : exception.Message;

        Error = exception;
        ErrorMessage = formatted;

        statusModel?.HandleError(exception, formatted, commandName, methodName);
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
/// <param name="errorFormatter">Optional formatter that maps an <see cref="Exception"/> to a user-facing string.</param>
public abstract class ObservableCommand(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommand
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
/// <param name="errorFormatter">Optional formatter that maps an <see cref="Exception"/> to a user-facing string.</param>
public class ObservableCommandFactory(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Action execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommand(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
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
/// <param name="errorFormatter">Optional formatter that maps an <see cref="Exception"/> to a user-facing string.</param>
public abstract class ObservableCommand<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommand<T>
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
/// <param name="errorFormatter">Optional formatter that maps an <see cref="Exception"/> to a user-facing string.</param>
public class ObservableCommandFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Action<T> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommand<T>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
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
