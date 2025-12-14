using ReactivePatternSample.Storage.Models;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Todo.Models;

/// <summary>
/// Commands and their implementations for TodoListModel.
/// </summary>
public partial class TodoListModel
{
    /// <summary>
    /// Command to add a new todo item.
    /// </summary>
    [ObservableCommand(nameof(AddItem), nameof(CanAddItem))]
    public partial IObservableCommand AddCommand { get; }

    /// <summary>
    /// Command to clear completed items.
    /// </summary>
    [ObservableCommand(nameof(ClearCompletedAsync))]
    public partial IObservableCommandAsync ClearCompletedCommand { get; }

    /// <summary>
    /// Can add item when authenticated and title is provided.
    /// </summary>
    private bool CanAddItem() =>
        Auth.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(NewItemTitle);

    /// <summary>
    /// Adds a new todo item for the current user.
    /// </summary>
    private void AddItem()
    {
        if (Auth.CurrentUser is null)
        {
            return;
        }

        var item = new TodoItem
        {
            Title = NewItemTitle.Trim(),
            Description = string.IsNullOrWhiteSpace(NewItemDescription) ? null : NewItemDescription.Trim(),
            OwnerId = Auth.CurrentUser.Id
        };

        Storage.AddItem(item);
        UserItems.Add(item);
        UpdateCounts();

        // Clear inputs
        NewItemTitle = string.Empty;
        NewItemDescription = null;

        Status.AddSuccess($"Added: {item.Title}", "Todo");
    }

    /// <summary>
    /// Clears all completed items with simulated delay.
    /// </summary>
    private async Task ClearCompletedAsync(CancellationToken ct)
    {
        IsBusy = true;

        try
        {
            var completedItems = UserItems.Where(i => i.IsCompleted).ToList();
            var count = completedItems.Count;

            // Simulate some processing time
            await Task.Delay(300, ct);

            foreach (var item in completedItems)
            {
                Storage.RemoveItem(item.Id);
                UserItems.Remove(item);
            }

            UpdateCounts();

            if (count > 0)
            {
                Status.AddSuccess($"Cleared {count} completed item(s)", "Todo");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
