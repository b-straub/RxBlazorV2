namespace ReactivePatternSample.Storage.Models;

/// <summary>
/// Application settings stored in memory.
/// Immutable record - use 'with' to create modified copies.
/// This ensures StorageModel.Settings setter is called, triggering StateHasChanged.
/// </summary>
public record AppSettings
{
    /// <summary>
    /// Preferred export format index (0=Plain, 1=Markdown, 2=Json).
    /// </summary>
    public int PreferredExportFormatIndex { get; init; }

    /// <summary>
    /// Theme mode: true = light (day), false = dark (night).
    /// </summary>
    public bool IsDayMode { get; init; } = true;
}
