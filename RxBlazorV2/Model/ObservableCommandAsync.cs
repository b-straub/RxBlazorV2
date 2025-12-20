using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public class ObservableCommandAsyncBase(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandAsyncBase
{
    public virtual bool Executing { get; protected set; }

    /// <summary>
    /// Reason for the last cancellation.
    /// </summary>
    public CancellationReason LastCancellationReason { get; protected set; }

    protected CancellationToken? CancellationToken => _cancellationTokenSource?.Token;
    private CancellationTokenSource? _cancellationTokenSource;

    protected void ResetCancellationToken(CancellationToken? externalToken)
    {
        LastCancellationReason = CancellationReason.NONE;

        if (_cancellationTokenSource is null || !_cancellationTokenSource.TryReset())
        {
            _cancellationTokenSource = externalToken is not null ? CancellationTokenSource.CreateLinkedTokenSource(externalToken.Value) : new();
        }
    }

    public virtual void Cancel()
    {
        ArgumentNullException.ThrowIfNull(_cancellationTokenSource);
        LastCancellationReason = CancellationReason.EXPLICIT;
        _cancellationTokenSource.Cancel();
    }
}

public abstract class ObservableCommandAsync(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandAsync
{
    public abstract Task ExecuteAsync(CancellationToken? externalCancellationToken);
    public abstract Task ExecuteAsync();
}

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

    public override async Task ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        await ExecuteAsync();
    }

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

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

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

    public override async Task ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken  = externalCancellationToken;
        await ExecuteAsync();
    }

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

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public abstract class ObservableCommandAsync<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel), IObservableCommandAsync<T>
{
    public abstract Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken);
    public abstract Task ExecuteAsync(T parameter);
}

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

    public override async Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken)
    {
        await ExecuteAsync(parameter);
    }

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

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

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

    public override async Task ExecuteAsync(T parameter, CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken  = externalCancellationToken;
        await ExecuteAsync(parameter);
    }

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

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
