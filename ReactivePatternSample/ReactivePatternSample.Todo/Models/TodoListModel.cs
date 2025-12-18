using ObservableCollections;
using ReactivePatternSample.Auth.Models;
using ReactivePatternSample.Status.Models;
using ReactivePatternSample.Storage.Models;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Todo.Models;

/// <summary>
/// Todo list domain model - manages todo items for the current user.
///
/// Patterns demonstrated:
/// - Singleton scope for shared state
/// - [ObservableComponent] for UI generation
/// - Partial constructor for cross-domain DI (Storage, Auth, Status)
/// - Internal observers (auto-detected) - RefreshItems() reacts to Auth.CurrentUser
/// - ObservableList for reactive filtered items view
///
/// File organization:
/// - TodoListModel.cs: Constructor and properties
/// - TodoListModel.Commands.cs: Commands with their implementations
/// - TodoListModel.Observers.cs: Internal observer methods
/// - TodoListModel.Methods.cs: Public API methods
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class TodoListModel : ObservableModel
{
    /// <summary>
    /// Partial constructor - generator creates DI injection for referenced models.
    /// </summary>
    public partial TodoListModel(StorageModel storage, AuthModel auth, AppStatusModel status);

    /// <summary>
    /// Filtered reactive collection of items for the current user.
    /// </summary>
    [ObservableComponentTrigger]
    public partial ObservableList<TodoItem> UserItems { get; init; } = [];

    /// <summary>
    /// Input for new todo title.
    /// </summary>
    public partial string NewItemTitle { get; set; } = string.Empty;

    /// <summary>
    /// Input for new todo description.
    /// </summary>
    public partial string? NewItemDescription { get; set; }

    /// <summary>
    /// Count of completed items.
    /// </summary>
    public partial int CompletedCount { get; set; }

    /// <summary>
    /// Count of pending (not completed) items.
    /// </summary>
    public partial int PendingCount { get; set; }

    /// <summary>
    /// Whether an operation is in progress.
    /// </summary>
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Current filter state.
    /// </summary>
    public partial TodoFilter CurrentFilter { get; set; } = TodoFilter.All;

    /// <summary>
    /// Item being edited (null if not in edit mode).
    /// </summary>
    public partial TodoItem? EditingItem { get; set; }
}

/// <summary>
/// Filter options for todo list.
/// </summary>
public enum TodoFilter
{
    All,
    Pending,
    Completed
}
