namespace ReactivePatternSample.Storage.Models;

/// <summary>
/// Todo item CRUD operations for StorageModel.
/// </summary>
public partial class StorageModel
{
    /// <summary>
    /// Adds a new todo item to storage.
    /// </summary>
    public void AddItem(TodoItem item)
    {
        Items.Add(item);
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a todo item from storage.
    /// </summary>
    public bool RemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        Items.Remove(item);
        LastModified = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Toggles the completion status of a todo item.
    /// </summary>
    public bool ToggleItemCompletion(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        item.IsCompleted = !item.IsCompleted;
        item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;

        // Force observable notification by triggering state change
        LastModified = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Updates an existing todo item.
    /// </summary>
    public bool UpdateItem(Guid itemId, string title, string? description)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        item.Title = title;
        item.Description = description;
        LastModified = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Gets all items for a specific user.
    /// </summary>
    public IEnumerable<TodoItem> GetItemsForUser(string userId)
    {
        return Items.Where(i => i.OwnerId == userId);
    }

    /// <summary>
    /// Clears all items for a specific user.
    /// </summary>
    public void ClearItemsForUser(string userId)
    {
        var userItems = Items.Where(i => i.OwnerId == userId).ToList();
        foreach (var item in userItems)
        {
            Items.Remove(item);
        }
        LastModified = DateTime.UtcNow;
    }
}
