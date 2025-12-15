using ReactivePatternSample.Storage.Models;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Settings.Models;

/// <summary>
/// Settings domain model - manages application preferences.
///
/// Patterns demonstrated:
/// - Singleton scope for shared settings across domains
/// - Connection to Storage domain for persistence
/// - [ObservableComponent] for settings UI generation
/// - [ObservableTrigger] for auto-save when settings change
///
/// File organization:
/// - SettingsModel.cs: Constructor and properties
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class SettingsModel : ObservableModel
{
    /// <summary>
    /// Partial constructor - generator creates DI injection for Storage.
    /// </summary>
    public partial SettingsModel(StorageModel storage);

    /// <summary>
    /// Preferred export format for sharing todo items.
    /// Changes are auto-saved to Storage via trigger.
    /// </summary>
    [ObservableTrigger(nameof(SavePreferredExportFormat))]
    public partial ExportFormat PreferredExportFormat { get; set; } = ExportFormat.Plain;

    /// <summary>
    /// Theme mode: true = light (day), false = dark (night).
    /// Changes are auto-saved to Storage via trigger.
    /// ObservableComponentTrigger enables MainLayout to react to theme changes.
    /// </summary>
    [ObservableComponentTrigger]
    [ObservableTrigger(nameof(SaveThemeMode))]
    public partial bool IsDay { get; set; } = true;

    /// <summary>
    /// Command to toggle between light and dark theme.
    /// </summary>
    [ObservableCommand(nameof(ToggleTheme))]
    public partial IObservableCommand ToggleThemeCommand { get; }

    private void ToggleTheme() => IsDay = !IsDay;

    /// <summary>
    /// Saves the preferred export format to storage when it changes.
    /// Uses 'with' to create new immutable AppSettings, triggering StateHasChanged on Storage.Settings.
    /// </summary>
    private void SavePreferredExportFormat()
    {
        Storage.Settings = Storage.Settings with { PreferredExportFormatIndex = (int)PreferredExportFormat };
    }

    /// <summary>
    /// Saves the theme mode to storage when it changes.
    /// </summary>
    private void SaveThemeMode()
    {
        Storage.Settings = Storage.Settings with { IsDayMode = IsDay };
    }

    /// <summary>
    /// Loads settings from storage when context is ready (async).
    /// Must be async to wait for StorageModel.OnContextReadyAsync to complete first.
    /// </summary>
    protected override Task OnContextReadyAsync()
    {
        PreferredExportFormat = (ExportFormat)Storage.Settings.PreferredExportFormatIndex;
        IsDay = Storage.Settings.IsDayMode;
        return Task.CompletedTask;
    }
}
