using ReactivePatternSample.Storage.Models;

namespace ReactivePatternSample.Todo.Models;

/// <summary>
/// Internal observer methods for TodoListModel.
/// These methods are auto-detected by the generator because they access
/// properties from referenced ObservableModel dependencies.
/// </summary>
public partial class TodoListModel
{
    /// <summary>
    /// Internal observer - auto-detected because it accesses Auth.CurrentUser.
    /// This method is automatically subscribed to Auth.Observable and called
    /// when Auth.CurrentUser changes.
    /// </summary>
    private void RefreshItems()
    {
        UserItems.Clear();

        if (Auth.CurrentUser is null)
        {
            UpdateCounts();
            return;
        }

        var items = Storage.GetItemsForUser(Auth.CurrentUser.Id);
        foreach (var item in FilterItems(items))
        {
            UserItems.Add(item);
        }

        UpdateCounts();
    }

    /// <summary>
    /// Filters items based on current filter state.
    /// </summary>
    private IEnumerable<TodoItem> FilterItems(IEnumerable<TodoItem> items) => CurrentFilter switch
    {
        TodoFilter.Completed => items.Where(i => i.IsCompleted),
        TodoFilter.Pending => items.Where(i => !i.IsCompleted),
        _ => items
    };

    /// <summary>
    /// Updates count properties.
    /// </summary>
    private void UpdateCounts()
    {
        CompletedCount = UserItems.Count(i => i.IsCompleted);
        PendingCount = UserItems.Count(i => !i.IsCompleted);
    }
}
