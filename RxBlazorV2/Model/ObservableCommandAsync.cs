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
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandAsyncBase
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
    /// Resets the cancellation token, optionally linking to an external token.
    /// </summary>
    protected void ResetCancellationToken(CancellationToken? externalToken)
    {
        LastCancellationReason = CancellationReason.NONE;

        if (_cancellationTokenSource is null || !_cancellationTokenSource.TryReset())
        {
            _cancellationTokenSource = externalToken is not null ? CancellationTokenSource.CreateLinkedTokenSource(externalToken.Value) : new();
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
    StatusBaseModel? statusModel = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandAsync
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
    StatusBaseModel? statusModel = null) :
    ObservableCommandAsync(model, observedProperties, commandName, methodName, statusModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        await ExecuteAsync();
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        Executing = true;
        _model.StateHasChanged(_observedProperties);

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
        _model.StateHasChanged(_observedProperties);
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
    StatusBaseModel? statusModel = null) :
    ObservableCommandAsync(model, observedProperties, commandName, methodName, statusModel)
{
    private readonly string[] _observedProperties = observedProperties;
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

        ResetCancellationToken(_externalCancellationToken);
        _externalCancellationToken = null;
        if (Executing)
        {
            LastCancellationReason = CancellationReason.SWITCH;
        }
        Executing = true;

        // If this is the first command in suspension, bypass suspension for immediate UI feedback
        if (_model.IsFirstCommandInSuspension())
        {
            _model.PropertyChangedSubject.OnNext(_observedProperties);
        }
        else
        {
            _model.StateHasChanged(_observedProperties);
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
            _model.StateHasChanged(_observedProperties);
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
    StatusBaseModel? statusModel = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandAsync<T>
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
    StatusBaseModel? statusModel = null) :
    ObservableCommandAsync<T>(model, observedProperties, commandName, methodName, statusModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    /// <inheritdoc />
    public override async Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken)
    {
        await ExecuteAsync(parameter);
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(T parameter)
    {
        Executing = true;
        _model.StateHasChanged(_observedProperties);

        SetError();
        try
        {
            await execute(parameter);
        }
        catch (Exception e)
        {
            SetError(e);
        }

        _model.StateHasChanged(_observedProperties);
        Executing = false;
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
    StatusBaseModel? statusModel = null) :
    ObservableCommandAsync<T>(model, observedProperties, commandName, methodName, statusModel)
{
    private readonly string[] _observedProperties = observedProperties;
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

        ResetCancellationToken(_externalCancellationToken);
        _externalCancellationToken = null;
        if (Executing)
        {
            LastCancellationReason = CancellationReason.SWITCH;
        }
        Executing = true;

        // If this is the first command in suspension, bypass suspension for immediate UI feedback
        if (_model.IsFirstCommandInSuspension())
        {
            _model.PropertyChangedSubject.OnNext(_observedProperties);
        }
        else
        {
            _model.StateHasChanged(_observedProperties);
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
            _model.StateHasChanged(_observedProperties);
        }
    }

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
