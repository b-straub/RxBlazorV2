using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public enum SuspensionState
{
    None,
    Active,
    Aborted
}

public abstract class ObservableModel : IObservableModel
{
    public abstract string ModelID { get; }

    private bool _initialized;
    private bool _initializedAsync;
    private int _suspendNotifications;
    private readonly HashSet<string> _pendingNotifications = new();
    private readonly Lock _suspenderLock = new();
    private NotificationSuspender? _currentSuspender;

    public Observable<string[]> Observable { get; }
    protected abstract IDisposable Subscriptions { get; }
    protected internal Subject<string[]> PropertyChangedSubject { get; } = new();

    protected ObservableModel()
    {
        Observable = PropertyChangedSubject
            .Publish()
            .RefCount();
    }

    public void ContextReady()
    {
        if (!_initialized)
        {
            OnContextReady();
            _initialized = true;
        }
    }
    
    public async Task ContextReadyAsync()
    {
        if (!_initializedAsync)
        {
            await OnContextReadyAsync();
            _initializedAsync = true;
        }
    }

    protected virtual void OnContextReady()
    {
    }
    
    protected virtual Task OnContextReadyAsync()
    {
        return Task.CompletedTask;
    }
    
    protected void StateHasChanged(string? propertyName = null)
    {
        if (_suspendNotifications > 0)
        {
            _pendingNotifications.Add(propertyName ?? ModelID);
            return;
        }
        PropertyChangedSubject.OnNext([propertyName ?? ModelID]);
    }

    protected internal void StateHasChanged(string[] propertyNames)
    {
        if (_suspendNotifications > 0)
        {
            foreach (var name in propertyNames.Length == 0 ? [ModelID] : propertyNames)
            {
                _pendingNotifications.Add(name);
            }
            return;
        }
        PropertyChangedSubject.OnNext(propertyNames.Length == 0 ? [ModelID] : propertyNames);
    }

    /// <summary>
    /// Suspends state change notifications until the returned IDisposable is disposed.
    /// Useful for batch updates to properties or observable collections.
    /// All property changes during suspension are batched and fired as a single notification when disposed.
    /// Supports nested calls - notifications are only fired when all suspensions are released.
    /// </summary>
    /// <example>
    /// <code>
    /// // Using statement (block scope):
    /// using (Model.SuspendNotifications())
    /// {
    ///     Tlist.Clear();
    ///     Tlist.AddRange(Enumerable.Range(0, 10000));
    ///     // Notification fires at end of block
    /// }
    ///
    /// 
    /// // Using declaration (method scope):
    /// void UpdateData()
    /// {
    ///     using var _ = Model.SuspendNotifications();
    ///     Tlist.Clear();
    ///     Tlist.AddRange(Enumerable.Range(0, 10000));
    ///     // Notification fires at end of method
    /// }
    /// </code>
    /// </example>
    /// <returns>IDisposable that fires batched notifications when disposed.</returns>
    public IDisposable SuspendNotifications()
    {
        lock (_suspenderLock)
        {
            var suspender = new NotificationSuspender(this);
            _currentSuspender = suspender;
            return suspender;
        }
    }

    internal bool IsFirstCommandInSuspension()
    {
        lock (_suspenderLock)
        {
            return _currentSuspender?.IsFirstCommand() ?? true;
        }
    }

    internal void AbortCurrentSuspension()
    {
        lock (_suspenderLock)
        {
            _currentSuspender?.Abort();
        }
    }

    internal bool IsSuspensionAborted()
    {
        lock (_suspenderLock)
        {
            return _currentSuspender?.IsAborted ?? false;
        }
    }

    public SuspensionState CurrentSuspensionState
    {
        get
        {
            lock (_suspenderLock)
            {
                if (_currentSuspender is null)
                {
                    return SuspensionState.None;
                }

                return _currentSuspender.IsAborted ? SuspensionState.Aborted : SuspensionState.Active;
            }
        }
    }

    private sealed class NotificationSuspender : IDisposable
    {
        private readonly ObservableModel _model;
        private bool _firstCommandExecuted;
        private bool _disposed;

        internal bool IsAborted { get; private set; }

        public NotificationSuspender(ObservableModel model)
        {
            _model = model;
            Interlocked.Increment(ref _model._suspendNotifications);
        }

        internal bool IsFirstCommand()
        {
            if (_firstCommandExecuted) return false;
            _firstCommandExecuted = true;
            return true;
        }

        internal void Abort()
        {
            IsAborted = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (Interlocked.Decrement(ref _model._suspendNotifications) == 0)
            {
                _model._currentSuspender = null;

                if (_model._pendingNotifications.Count > 0)
                {
                    _model.PropertyChangedSubject.OnNext(_model._pendingNotifications.ToArray());
                    _model._pendingNotifications.Clear();
                }
            }
        }
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Subscriptions.Dispose();
    }
}