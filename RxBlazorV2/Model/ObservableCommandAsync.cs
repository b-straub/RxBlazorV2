using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

/// <summary>
/// Base class for async commands with execution tracking and cancellation support.
/// </summary>
public class ObservableCommandAsyncBase(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandAsyncBase
{
    /// <summary>
    /// Indicates whether the command is currently executing.
    /// </summary>
    public virtual bool Executing { get; protected set; }

    /// <summary>
    /// Reason for the last cancellation.
    /// </summary>
    public CancellationReason LastCancellationReason { get; protected set; }

    /// <summary>
    /// Gets the current cancellation token for the executing command.
    /// </summary>
    protected CancellationToken? CancellationToken => _cancellationTokenSource?.Token;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Prepares a cancellation token for a new execution, optionally linking to an external token.
    /// </summary>
    /// <param name="externalToken">Token the new source is linked to, when the caller supplied one.</param>
    /// <param name="isSwitch">
    /// True when a previous execution is still in flight. That run is cancelled and its
    /// <see cref="LastCancellationReason"/> reported as <see cref="CancellationReason.SWITCH"/>,
    /// so the run replacing it keeps <see cref="Executing"/> set.
    /// </param>
    protected void ResetCancellationToken(CancellationToken? externalToken, bool isSwitch)
    {
        var previous = _cancellationTokenSource;

        LastCancellationReason = isSwitch ? CancellationReason.SWITCH : CancellationReason.NONE;

        // TryReset() drops every registration on the source, which silently unhooks an in-flight
        // Task.Delay(..., token) from the token it was handed. Only reuse a source that no
        // execution is still holding, otherwise the switched-away run becomes uncancellable.
        if (isSwitch || previous is null || !previous.TryReset())
        {
            _cancellationTokenSource = externalToken is not null ? CancellationTokenSource.CreateLinkedTokenSource(externalToken.Value) : new();
        }

        if (isSwitch && previous is not null)
        {
            // Cancel only once the new source is in place: cancelling can resume the previous run
            // synchronously, and its finally block must not observe the source it was cancelled on.
            // The source is left to the GC - the cancelled run may still touch its token while
            // unwinding, and disposing it here would turn that into an ObjectDisposedException.
            previous.Cancel();
        }
    }

    /// <summary>
    /// Cancels the currently executing command.
    /// </summary>
    public virtual void Cancel()
    {
        ArgumentNullException.ThrowIfNull(_cancellationTokenSource);
        LastCancellationReason = CancellationReason.EXPLICIT;
        _cancellationTokenSource.Cancel();
    }
}

/// <summary>
/// Abstract async command without parameters.
/// </summary>
public abstract class ObservableCommandAsync(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandAsync
{
    /// <summary>
    /// Executes the command asynchronously with an optional external cancellation token.
    /// </summary>
    public abstract Task ExecuteAsync(CancellationToken? externalCancellationToken);

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    public abstract Task ExecuteAsync();
}

/// <summary>
/// Non-cancellable async command that executes a delegate without cancellation support.
/// </summary>
public class ObservableCommandAsyncFactory(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<Task> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandAsync(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        await ExecuteAsync();
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        Executing = true;
        NotifyStateChanged();

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
        NotifyStateChanged();
    }

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Cancellable async command that supports CancellationToken and switch-cancellation.
/// </summary>
public class ObservableCommandAsyncCancelableFactory(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<CancellationToken, Task> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandAsync(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly ObservableModel _model = model;
    private CancellationToken? _externalCancellationToken;

    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken  = externalCancellationToken;
        await ExecuteAsync();
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        // Early exit if suspension already aborted
        if (_model.IsSuspensionAborted())
        {
            return;
        }

        // A run already in flight is switched away from: it gets cancelled, not abandoned.
        var isSwitch = Executing;
        ResetCancellationToken(_externalCancellationToken, isSwitch);
        _externalCancellationToken = null;
        Executing = true;

        // If this is the first command in suspension, bypass suspension for immediate UI feedback
        if (_model.IsFirstCommandInSuspension())
        {
            _model.PropertyChangedSubject.OnNext(StateChangeProperties);
        }
        else
        {
            NotifyStateChanged();
        }

        SetError();
        try
        {
            if (!CancellationToken.HasValue)
            {
                throw new InvalidOperationException("CancellationToken must be set!");
            }
            await execute(CancellationToken.Value);
        }
        catch (TaskCanceledException)
        {
            _model.AbortCurrentSuspension();
        }
        catch (OperationCanceledException)
        {
            _model.AbortCurrentSuspension();
        }
        catch (Exception e)
        {
            SetError(e);
        }
        finally
        {
            // For SWITCH cancellations, don't clear Executing - new command is starting
            if (LastCancellationReason != CancellationReason.SWITCH)
            {
                Executing = false;
            }
            else
            {
                LastCancellationReason = CancellationReason.NONE;
            }
            NotifyStateChanged();
        }
    }

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Abstract parametrized async command that accepts a parameter of type <typeparamref name="T"/>.
/// </summary>
public abstract class ObservableCommandAsync<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandAsync<T>
{
    /// <summary>
    /// Executes the command asynchronously with the given parameter and optional external cancellation token.
    /// </summary>
    public abstract Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken);

    /// <summary>
    /// Executes the command asynchronously with the given parameter.
    /// </summary>
    public abstract Task ExecuteAsync(T parameter);
}

/// <summary>
/// Non-cancellable parametrized async command that executes a delegate with a parameter.
/// </summary>
public class ObservableCommandAsyncFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T, Task> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandAsync<T>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken)
    {
        await ExecuteAsync(parameter);
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(T parameter)
    {
        Executing = true;
        NotifyStateChanged();

        SetError();
        try
        {
            await execute(parameter);
        }
        catch (Exception e)
        {
            SetError(e);
        }

        Executing = false;
        NotifyStateChanged();
    }

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Cancellable parametrized async command that supports CancellationToken and switch-cancellation.
/// </summary>
public class ObservableCommandAsyncCancelableFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T, CancellationToken, Task> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandAsync<T>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly ObservableModel _model = model;
    private CancellationToken? _externalCancellationToken;

    /// <inheritdoc />
    public override async Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken  = externalCancellationToken;
        await ExecuteAsync(parameter);
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(T parameter)
    {
        // Early exit if suspension already aborted
        if (_model.IsSuspensionAborted())
        {
            return;
        }

        // A run already in flight is switched away from: it gets cancelled, not abandoned.
        var isSwitch = Executing;
        ResetCancellationToken(_externalCancellationToken, isSwitch);
        _externalCancellationToken = null;
        Executing = true;

        // If this is the first command in suspension, bypass suspension for immediate UI feedback
        if (_model.IsFirstCommandInSuspension())
        {
            _model.PropertyChangedSubject.OnNext(StateChangeProperties);
        }
        else
        {
            NotifyStateChanged();
        }

        SetError();
        try
        {
            if (!CancellationToken.HasValue)
            {
                throw new InvalidOperationException("CancellationToken must be set!");
            }
            await execute(parameter, CancellationToken.Value);
        }
        catch (TaskCanceledException)
        {
            _model.AbortCurrentSuspension();
        }
        catch (OperationCanceledException)
        {
            _model.AbortCurrentSuspension();
        }
        catch (Exception e)
        {
            SetError(e);
        }
        finally
        {
            // For SWITCH cancellations, don't clear Executing - new command is starting
            if (LastCancellationReason != CancellationReason.SWITCH)
            {
                Executing = false;
            }
            else
            {
                LastCancellationReason = CancellationReason.NONE;
            }
            NotifyStateChanged();
        }
    }

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
