using ObservableCollections;
using ReactivePatternSample.Storage.Services;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Storage.Models;

/// <summary>
/// Storage domain model - UI-less in-memory database using R3 reactive collections.
///
/// Patterns demonstrated:
/// - UI-less ObservableModel (no [ObservableComponent])
/// - Singleton scope for shared data
/// - R3 ObservableList&lt;T&gt; for reactive collections
/// - OnContextReady for seed data initialization
/// - [ObservableModelObserver] for external persistence service
///
/// File organization:
/// - StorageModel.cs: Properties and lifecycle
/// - StorageModel.Items.cs: Todo item CRUD operations
/// - StorageModel.Users.cs: User authentication and seed data
/// </summary>
[ObservableModelScope(ModelScope.Singleton)]
public partial class StorageModel : ObservableModel
{
    /// <summary>
    /// Injects the persistence observer service.
    /// The generator auto-subscribes methods marked with [ObservableModelObserver].
    /// </summary>
    public partial StorageModel(StoragePersistenceObserver persistenceObserver);

    #region Properties

    /// <summary>
    /// Reactive collection of todo items.
    /// Changes to this collection are automatically propagated to observers.
    /// </summary>
    [ObservableTrigger(nameof(UpdateCounts))]
    public partial ObservableList<TodoItem> Items { get; init; } = [];

    /// <summary>
    /// Reactive collection of users.
    /// Seeded with demo users in OnContextReady.
    /// </summary>
    public partial ObservableList<User> Users { get; init; } = [];

    /// <summary>
    /// Tracks total item count for reactive updates.
    /// </summary>
    public partial int TotalItemCount { get; set; }

    /// <summary>
    /// Tracks completed item count for reactive updates.
    /// </summary>
    public partial int CompletedItemCount { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public partial DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Application settings stored in memory.
    /// In a real application, this would be persisted to local storage or a database.
    /// </summary>
    public partial AppSettings Settings { get; set; } = new();

    #endregion

    #region Lifecycle

    /// <summary>
    /// Seeds demo users when context is ready.
    /// Called via generated OnContextReadyIntern from referencing models.
    /// </summary>
    protected override void OnContextReady()
    {
        SeedUsers();
    }

    /// <summary>
    /// Loads persisted data from local storage on startup.
    /// Uses SuspendNotifications to batch all changes into one notification.
    /// Only loads if Items is empty (prevents duplicate loading on multiple calls).
    /// </summary>
    protected override async Task OnContextReadyAsync()
    {
        // Use SuspendNotifications to batch all property changes
        // This fires ONE notification at the end instead of multiple
        using (SuspendNotifications())
        {
            var (items, settings) = await PersistenceObserver.LoadPersistedDataAsync();

            if (settings is not null)
            {
                Settings = settings;
            }
            
            if (items is { Count: > 0 })
            {
                Items.AddRange(items);
            }
        }
    }

    #endregion

    #region Internal

    /// <summary>
    /// Updates the count properties based on current items.
    /// This triggers reactive updates to observers.
    /// </summary>
    private void UpdateCounts()
    {
        TotalItemCount = Items.Count;
        CompletedItemCount = Items.Count(i => i.IsCompleted);
    }

    #endregion
}
