using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public abstract class ObservableModel : IObservableModel
{
    public abstract string ModelID { get; }

    private bool _initialized;
    private bool _initializedAsync;
    private bool _suspendNotifications;
    private readonly HashSet<string> _pendingNotifications = new();
    private readonly Lock _suspenderLock = new();
    private NotificationSuspender? _currentSuspender;
    private readonly HashSet<string> _suspendedBatchIds = new();

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
    
    protected void StateHasChanged(string propertyName, params string[] batchIds)
    {
        StateHasChanged([propertyName], batchIds);
    }

    protected internal void StateHasChanged(string[] propertyNames, params string[] batchIds)
    {
        if (propertyNames.Length == 0)
        {
            throw new InvalidOperationException("State has changed without a properties!");
        }

        var isSuspended = _suspendNotifications && (_suspendedBatchIds.Count == 0 || batchIds.Any(id => _suspendedBatchIds.Contains(id)));

        if (isSuspended)
        {
            foreach (var name in propertyNames)
            {
                _pendingNotifications.Add(name);
            }
            return;
        }

        PropertyChangedSubject.OnNext(propertyNames);
    }

    /// <summary>
    /// Suspends state change notifications until the returned IDisposable is disposed.
    /// Useful for batch updates to properties or observable collections.
    /// All property changes during suspension are batched and fired as a single notification when disposed.
    /// </summary>
    /// <param name="batchIds">
    /// Optional batch identifiers. When provided, only suspends notifications for properties with matching
    /// <see cref="ObservableBatchAttribute"/>. When empty, suspends all notifications.
    /// Multiple batch IDs can be specified to suspend multiple batch groups simultaneously.
    /// </param>
    /// <remarks>
    /// <para>Nested suspensions are not supported and will throw <see cref="InvalidOperationException"/>.</para>
    /// <para>Properties without batch IDs are suspended when no batch IDs are specified.</para>
    /// <para>Properties with batch IDs are only suspended if at least one of their batch IDs matches a suspended batch.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Suspend all notifications:
    /// using (Model.SuspendNotifications())
    /// {
    ///     Tlist.Clear();
    ///     Tlist.AddRange(Enumerable.Range(0, 10000));
    ///     // All notifications fire at end of block
    /// }
    ///
    /// // Single batch-specific suspension:
    /// using (Model.SuspendNotifications("UserInfo"))
    /// {
    ///     FirstName = "John";  // Suspended if [ObservableBatch("UserInfo")]
    ///     LastName = "Doe";     // Suspended if [ObservableBatch("UserInfo")]
    ///     Age = 30;             // NOT suspended if no [ObservableBatch] or different batch ID
    /// }
    ///
    /// // Multiple batch suspensions:
    /// using (Model.SuspendNotifications("UserInfo", "Address"))
    /// {
    ///     FirstName = "John";   // Suspended if [ObservableBatch("UserInfo")]
    ///     City = "New York";    // Suspended if [ObservableBatch("Address")]
    /// }
    /// </code>
    /// </example>
    /// <returns>IDisposable that fires batched notifications when disposed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a suspension is already active.</exception>
    public IDisposable SuspendNotifications(params string[] batchIds)
    {
        lock (_suspenderLock)
        {
            if (_suspendNotifications)
            {
                throw new InvalidOperationException("SuspendNotifications is already active. Nested suspensions are not supported.");
            }

            var suspender = new NotificationSuspender(this, batchIds);
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

    private sealed class NotificationSuspender : IDisposable
    {
        private readonly ObservableModel _model;
        private bool _firstCommandExecuted;
        private bool _disposed;

        internal bool IsAborted { get; private set; }

        public NotificationSuspender(ObservableModel model, string[] batchIds)
        {
            _model = model;

            _model._suspendNotifications = true;

            foreach (var batchId in batchIds)
            {
                _model._suspendedBatchIds.Add(batchId);
            }
        }

        internal bool IsFirstCommand()
        {
            if (_firstCommandExecuted)
            {
                return false;
            }
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

            _model._suspendedBatchIds.Clear();
            _model._suspendNotifications = false;
            _model._currentSuspender = null;

            if (_model._pendingNotifications.Count > 0)
            {
                _model.PropertyChangedSubject.OnNext(_model._pendingNotifications.ToArray());
                _model._pendingNotifications.Clear();
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
        if (disposing)
        {
            Subscriptions.Dispose();
            PropertyChangedSubject.Dispose();
        }
    }
}