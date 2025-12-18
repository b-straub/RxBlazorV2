using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public abstract class ObservableCommandRAsync<T>(ObservableModel model, string[] observedProperties, IErrorModel? errorModel = null)
    : ObservableCommandAsyncBase(model, observedProperties, errorModel), IObservableCommandRAsync<T>
{
    public abstract Task<T?> ExecuteAsync(CancellationToken? externalCancellationToken);
    public abstract Task<T?> ExecuteAsync();
}

public class ObservableCommandRAsyncFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    Func<Task<T?>> execute,
    Func<bool>? canExecute = null,
    IErrorModel? errorModel = null) :
    ObservableCommandRAsync<T>(model, observedProperties, errorModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override async Task<T?> ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        return await ExecuteAsync();
    }

    public override async Task<T?> ExecuteAsync()
    {
        T? result = default;
        Executing = true;
        _model.StateHasChanged(_observedProperties);

        SetError();
        try
        {
            result = await execute();
        }
        catch (Exception e)
        {
            SetError(e);
        }

        Executing = false;
        _model.StateHasChanged(_observedProperties);
        return result;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public class ObservableCommandRAsyncCancelableFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    Func<CancellationToken, Task<T?>> execute,
    Func<bool>? canExecute = null,
    IErrorModel? errorModel = null) :
    ObservableCommandRAsync<T>(model, observedProperties, errorModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;
    private CancellationToken? _externalCancellationToken;

    public override async Task<T?> ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken = externalCancellationToken;
        return await ExecuteAsync();
    }

    public override async Task<T?> ExecuteAsync()
    {
        T? result = default;

        // Early exit if suspension already aborted
        if (_model.IsSuspensionAborted())
        {
            return result;
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
            result = await execute(CancellationToken.Value);
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

        return result;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public abstract class ObservableCommandRAsync<T1, T2>(ObservableModel model, string[] observedProperties, IErrorModel? errorModel = null)
    : ObservableCommandAsyncBase(model, observedProperties, errorModel), IObservableCommandRAsync<T1, T2>
{
    public abstract Task<T2?> ExecuteAsync(T1 parameter, CancellationToken? externalCancellationToken);
    public abstract Task<T2?> ExecuteAsync(T1 parameter);
}

public class ObservableCommandRAsyncFactory<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    Func<T1, Task<T2?>> execute,
    Func<bool>? canExecute = null,
    IErrorModel? errorModel = null) :
    ObservableCommandRAsync<T1, T2>(model, observedProperties, errorModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    public override async Task<T2?> ExecuteAsync(T1 parameter, CancellationToken? externalCancellationToken)
    {
        return await ExecuteAsync(parameter);
    }

    public override async Task<T2?> ExecuteAsync(T1 parameter)
    {
        T2? result = default;
        Executing = true;
        _model.StateHasChanged(_observedProperties);

        SetError();
        try
        {
            result = await execute(parameter);
        }
        catch (Exception e)
        {
            SetError(e);
        }

        Executing = false;
        _model.StateHasChanged(_observedProperties);
        return result;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

public class ObservableCommandRAsyncCancelableFactory<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    Func<T1, CancellationToken, Task<T2?>> execute,
    Func<bool>? canExecute = null,
    IErrorModel? errorModel = null) :
    ObservableCommandRAsync<T1, T2>(model, observedProperties, errorModel)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;
    private CancellationToken? _externalCancellationToken;

    public override async Task<T2?> ExecuteAsync(T1 parameter, CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken = externalCancellationToken;
        return await ExecuteAsync(parameter);
    }

    public override async Task<T2?> ExecuteAsync(T1 parameter)
    {
        T2? result = default;

        // Early exit if suspension already aborted
        if (_model.IsSuspensionAborted())
        {
            return result;
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
            result = await execute(parameter, CancellationToken.Value);
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

        return result;
    }

    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
