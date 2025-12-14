using ReactivePatternSample.Storage.Models;

namespace ReactivePatternSample.Todo.Models;

/// <summary>
/// Public API methods for TodoListModel.
/// </summary>
public partial class TodoListModel
{
    /// <summary>
    /// Toggles the completion status of an item.
    /// </summary>
    public void ToggleItem(Guid itemId)
    {
        if (Storage.ToggleItemCompletion(itemId))
        {
            // Find and update local item
            var item = UserItems.FirstOrDefault(i => i.Id == itemId);
            if (item is not null)
            {
                UpdateCounts();
                Status.AddInfo($"Toggled: {item.Title}", "Todo");
            }
        }
    }

    /// <summary>
    /// Removes an item.
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        var item = UserItems.FirstOrDefault(i => i.Id == itemId);
        if (item is not null && Storage.RemoveItem(itemId))
        {
            UserItems.Remove(item);
            UpdateCounts();
            Status.AddInfo($"Removed: {item.Title}", "Todo");
        }
    }

    /// <summary>
    /// Sets the filter and refreshes the list.
    /// </summary>
    public void SetFilter(TodoFilter filter)
    {
        CurrentFilter = filter;
        RefreshItems();
    }

    /// <summary>
    /// Starts editing an item.
    /// </summary>
    public void StartEdit(TodoItem item)
    {
        EditingItem = item;
    }

    /// <summary>
    /// Saves edits to an item.
    /// </summary>
    public void SaveEdit(string title, string? description)
    {
        if (EditingItem is null)
        {
            return;
        }

        if (Storage.UpdateItem(EditingItem.Id, title, description))
        {
            EditingItem.Title = title;
            EditingItem.Description = description;
            Status.AddSuccess($"Updated: {title}", "Todo");
        }

        EditingItem = null;
    }

    /// <summary>
    /// Cancels editing.
    /// </summary>
    public void CancelEdit()
    {
        EditingItem = null;
    }
}
