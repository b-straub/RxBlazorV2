using ReactivePatternSample.Storage.Models;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Storage.Services;

/// <summary>
/// External model observer that auto-persists StorageModel data to local storage.
///
/// Demonstrates the [ObservableModelObserver] pattern:
/// - Decouples persistence logic from the model
/// - Automatically triggers when observed properties change
/// - Supports async operations with CancellationToken
/// - Can observe multiple properties with a single method
/// </summary>
public class StoragePersistenceObserver(LocalStorageService localStorage)
{
    private const string TodoItemsKey = "rxblazor-todo-items";
    private const string AppSettingsKey = "rxblazor-app-settings";

    /// <summary>
    /// Called when the Items collection changes.
    /// Persists all todo items to local storage.
    /// </summary>
    [ObservableModelObserver(nameof(StorageModel.Items))]
    public async Task OnItemsChangedAsync(StorageModel model, CancellationToken ct)
    {
        // Convert ObservableList to regular list for serialization
        var items = model.Items.ToList();
        await localStorage.SetItemAsync(TodoItemsKey, items, ct);
    }

    /// <summary>
    /// Called when app settings change.
    /// Persists settings to local storage.
    /// </summary>
    [ObservableModelObserver(nameof(StorageModel.Settings))]
    public async Task OnSettingsChangedAsync(StorageModel model, CancellationToken ct)
    {
        await localStorage.SetItemAsync(AppSettingsKey, model.Settings, ct);
    }

    /// <summary>
    /// Loads persisted data from local storage.
    /// Called during application startup to restore state.
    /// </summary>
    public async Task<(List<TodoItem>? items, AppSettings? settings)> LoadPersistedDataAsync(CancellationToken ct = default)
    {
        var items = await localStorage.GetItemAsync<List<TodoItem>>(TodoItemsKey, ct);
        var settings = await localStorage.GetItemAsync<AppSettings>(AppSettingsKey, ct);
        return (items, settings);
    }
}
