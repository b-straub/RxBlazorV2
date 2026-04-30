using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

/// <summary>
/// Abstract base class for asynchronous observable commands that return a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The return type of the command.</typeparam>
public abstract class ObservableCommandRAsync<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandRAsync<T>
{
    /// <summary>
    /// Executes the command asynchronously with an optional external cancellation token and returns the result.
    /// </summary>
    /// <param name="externalCancellationToken">An optional external cancellation token.</param>
    /// <returns>The result of the command execution, or default if execution fails.</returns>
    public abstract Task<T?> ExecuteAsync(CancellationToken? externalCancellationToken);

    /// <summary>
    /// Executes the command asynchronously and returns the result.
    /// </summary>
    /// <returns>The result of the command execution, or default if execution fails.</returns>
    public abstract Task<T?> ExecuteAsync();
}

/// <summary>
/// Non-cancellable asynchronous observable command that returns a value of type <typeparamref name="T"/>, backed by a delegate.
/// </summary>
/// <typeparam name="T">The return type of the command.</typeparam>
public class ObservableCommandRAsyncFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<Task<T?>> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandRAsync<T>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    /// <inheritdoc />
    public override async Task<T?> ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        return await ExecuteAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Cancellable asynchronous observable command that returns a value of type <typeparamref name="T"/>, backed by a delegate that accepts a <see cref="CancellationToken"/>.
/// </summary>
/// <typeparam name="T">The return type of the command.</typeparam>
public class ObservableCommandRAsyncCancelableFactory<T>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<CancellationToken, Task<T?>> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandRAsync<T>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;
    private CancellationToken? _externalCancellationToken;

    /// <inheritdoc />
    public override async Task<T?> ExecuteAsync(CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken = externalCancellationToken;
        return await ExecuteAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Abstract base class for asynchronous observable commands that accept a parameter of type <typeparamref name="T1"/> and return a value of type <typeparamref name="T2"/>.
/// </summary>
/// <typeparam name="T1">The parameter type of the command.</typeparam>
/// <typeparam name="T2">The return type of the command.</typeparam>
public abstract class ObservableCommandRAsync<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null)
    : ObservableCommandAsyncBase(model, observedProperties, commandName, methodName, statusModel, errorFormatter), IObservableCommandRAsync<T1, T2>
{
    /// <summary>
    /// Executes the command asynchronously with the specified parameter and an optional external cancellation token.
    /// </summary>
    /// <param name="parameter">The input parameter for the command.</param>
    /// <param name="externalCancellationToken">An optional external cancellation token.</param>
    /// <returns>The result of the command execution, or default if execution fails.</returns>
    public abstract Task<T2?> ExecuteAsync(T1 parameter, CancellationToken? externalCancellationToken);

    /// <summary>
    /// Executes the command asynchronously with the specified parameter and returns the result.
    /// </summary>
    /// <param name="parameter">The input parameter for the command.</param>
    /// <returns>The result of the command execution, or default if execution fails.</returns>
    public abstract Task<T2?> ExecuteAsync(T1 parameter);
}

/// <summary>
/// Non-cancellable asynchronous observable command that accepts a parameter of type <typeparamref name="T1"/> and returns a value of type <typeparamref name="T2"/>, backed by a delegate.
/// </summary>
/// <typeparam name="T1">The parameter type of the command.</typeparam>
/// <typeparam name="T2">The return type of the command.</typeparam>
public class ObservableCommandRAsyncFactory<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T1, Task<T2?>> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandRAsync<T1, T2>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;

    /// <inheritdoc />
    public override async Task<T2?> ExecuteAsync(T1 parameter, CancellationToken? externalCancellationToken)
    {
        return await ExecuteAsync(parameter);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}

/// <summary>
/// Cancellable asynchronous observable command that accepts a parameter of type <typeparamref name="T1"/> and returns a value of type <typeparamref name="T2"/>, backed by a delegate that accepts a <see cref="CancellationToken"/>.
/// </summary>
/// <typeparam name="T1">The parameter type of the command.</typeparam>
/// <typeparam name="T2">The return type of the command.</typeparam>
public class ObservableCommandRAsyncCancelableFactory<T1, T2>(
    ObservableModel model,
    string[] observedProperties,
    string commandName,
    string methodName,
    Func<T1, CancellationToken, Task<T2?>> execute,
    Func<bool>? canExecute = null,
    StatusBaseModel? statusModel = null,
    Func<Exception, string>? errorFormatter = null) :
    ObservableCommandRAsync<T1, T2>(model, observedProperties, commandName, methodName, statusModel, errorFormatter)
{
    private readonly string[] _observedProperties = observedProperties;
    private readonly ObservableModel _model = model;
    private CancellationToken? _externalCancellationToken;

    /// <inheritdoc />
    public override async Task<T2?> ExecuteAsync(T1 parameter, CancellationToken? externalCancellationToken)
    {
        _externalCancellationToken = externalCancellationToken;
        return await ExecuteAsync(parameter);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool CanExecute => canExecute?.Invoke() ?? true;
}
