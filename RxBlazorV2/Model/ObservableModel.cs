using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

/// <summary>
/// Abstract base class for all reactive models, providing observable state management using R3.
/// </summary>
public abstract class ObservableModel : IObservableModel
{
    /// <summary>
    /// Gets the unique identifier for this model, used in property change notifications.
    /// </summary>
    public abstract string ModelID { get; }

    /// <summary>
    /// Returns true if any of the specified property names are used by this model's subscribers.
    /// </summary>
    public abstract bool FilterUsedProperties(params string[] propertyNames);
    
    private bool _initialized;
    private bool _initializedAsync;
    private readonly SemaphoreSlim _contextReadyAsyncLock = new(1, 1);
    private bool _suspendNotifications;
    private readonly HashSet<string> _pendingNotifications = new();
    private readonly Lock _suspenderLock = new();
    private NotificationSuspender? _currentSuspender;
    private readonly HashSet<string> _suspendedBatchIds = new();

    /// <summary>
    /// Gets the observable stream of property change notifications, emitting arrays of changed property names.
    /// </summary>
    public Observable<string[]> Observable { get; }

    /// <summary>
    /// Gets the composite disposable that manages all reactive subscriptions for this model.
    /// </summary>
    public CompositeDisposable Subscriptions { get; }

    /// <summary>
    /// Gets a value indicating whether both sync and async context initialization have completed.
    /// </summary>
    public bool Initialized => _initialized && _initializedAsync;

    /// <summary>
    /// Gets the subject used to publish property change notifications.
    /// </summary>
    protected internal Subject<string[]> PropertyChangedSubject { get; } = new();

    /// <summary>
    /// Initializes the observable stream and subscription container.
    /// </summary>
    protected ObservableModel()
    {
        Observable = PropertyChangedSubject
            .Publish()
            .RefCount();

        Subscriptions = new CompositeDisposable();
    }

    /// <summary>
    /// Performs synchronous initialization, ensuring it runs only once.
    /// </summary>
    public void ContextReady()
    {
        if (!_initialized)
        {
            OnContextReadyIntern();
            OnContextReady();
            _initialized = true;
        }
    }
    
    /// <summary>
    /// Performs asynchronous initialization with thread-safe locking, ensuring it runs only once.
    /// </summary>
    public async Task ContextReadyAsync()
    {
        await _contextReadyAsyncLock.WaitAsync();
        try
        {
            if (!_initializedAsync)
            {
                await OnContextReadyInternAsync();
                await OnContextReadyAsync();
                _initializedAsync = true;
            }
        }
        finally
        {
            _contextReadyAsyncLock.Release();
        }
    }
    
    /// <summary>
    /// Called during synchronous initialization for generated code setup; override in generated code only.
    /// </summary>
    protected virtual void OnContextReadyIntern()
    {
    }

    /// <summary>
    /// Called during synchronous initialization; override to perform custom setup logic.
    /// </summary>
    protected virtual void OnContextReady()
    {
    }

    /// <summary>
    /// Called during asynchronous initialization for generated code setup; override in generated code only.
    /// </summary>
    protected virtual Task OnContextReadyInternAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called during asynchronous initialization; override to perform custom async setup logic.
    /// </summary>
    protected virtual Task OnContextReadyAsync()
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Notifies subscribers that a single property has changed.
    /// </summary>
    protected void StateHasChanged(string propertyName, params string[] batchIds)
    {
        StateHasChanged([propertyName], batchIds);
    }

    /// <summary>
    /// Notifies subscribers that one or more properties have changed, respecting active suspensions.
    /// </summary>
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

    /// <summary>
    /// Returns true if this is the first command execution within the current suspension scope.
    /// </summary>
    internal bool IsFirstCommandInSuspension()
    {
        lock (_suspenderLock)
        {
            return _currentSuspender?.IsFirstCommand() ?? true;
        }
    }

    /// <summary>
    /// Marks the current notification suspension as aborted, preventing batched notifications on dispose.
    /// </summary>
    internal void AbortCurrentSuspension()
    {
        lock (_suspenderLock)
        {
            _currentSuspender?.Abort();
        }
    }

    /// <summary>
    /// Returns true if the current notification suspension has been aborted.
    /// </summary>
    internal bool IsSuspensionAborted()
    {
        lock (_suspenderLock)
        {
            return _currentSuspender?.IsAborted ?? false;
        }
    }

    /// <summary>
    /// Manages a notification suspension scope, batching property changes until disposed.
    /// </summary>
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
    
    /// <summary>
    /// Disposes subscriptions and the property changed subject.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed resources when disposing is true.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Subscriptions.Dispose();
            PropertyChangedSubject.Dispose();
        }
    }
}