using Microsoft.JSInterop;
using System.Text.Json;

namespace ReactivePatternSample.Storage.Services;

/// <summary>
/// Service for persisting data to browser local storage.
/// Provides async methods for JSON serialization/deserialization.
/// </summary>
public class LocalStorageService(IJSRuntime jsRuntime)
{
    /// <summary>
    /// Saves an item to local storage.
    /// </summary>
    public async ValueTask SetItemAsync<T>(string key, T value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", ct, key, json);
    }

    /// <summary>
    /// Retrieves an item from local storage.
    /// Returns default if not found or on error.
    /// </summary>
    public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ct, key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Removes an item from local storage.
    /// </summary>
    public async ValueTask RemoveItemAsync(string key, CancellationToken ct = default)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", ct, key);
    }

    /// <summary>
    /// Checks if a key exists in local storage.
    /// </summary>
    public async ValueTask<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
    {
        var value = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ct, key);
        return value is not null;
    }
}
