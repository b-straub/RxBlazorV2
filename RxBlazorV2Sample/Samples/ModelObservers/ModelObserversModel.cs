using ObservableCollections;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ModelObservers;

/// <summary>
/// Demonstrates model observers - both internal (auto-detected) and external (attribute-based).
///
/// Internal observers: Private methods that access injected ObservableModel properties are auto-detected.
/// External observers: Service methods with [ObservableModelObserver] are subscribed via OnContextReadyIntern().
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ModelObserversModel : SampleBaseModel
{
    public override string Usage => "Model observers: Internal (auto-detected private methods) and External ([ObservableModelObserver] on services)";

    public partial string CurrentUser { get; set; } = "Guest";

    public partial string Settings { get; set; } = "{}";

    public partial string Theme { get; set; } = "Light";

    public partial int NotificationCount { get; set; }

    // For displaying observer execution results
    public partial ObservableList<string> ObserverResults { get; init; } = [];

    // Inject:
    // - TimerSettingsModel: ObservableModel for demonstrating internal observers
    // - ModelObserversService: Service with [ObservableModelObserver] for external observers
    public partial ModelObserversModel(TimerSettingsModel timerSettings, ModelObserversService service);

    // ============================================
    // INTERNAL MODEL OBSERVERS (Auto-Detected)
    // ============================================
    // These private methods access TimerSettings.TickCount and TimerSettings.IsRunning.
    // The generator auto-detects this and creates subscriptions in the constructor.

    /// <summary>
    /// Internal observer: Auto-detected sync method observing TimerSettings.TickCount
    /// Called automatically when TickCount changes.
    /// </summary>
    private void OnTickCountChanged()
    {
        ObserverResults.Add($"[Internal-Sync] Tick count changed to: {TimerSettings.TickCount} at {DateTime.Now:HH:mm:ss}");
    }

    /// <summary>
    /// Internal observer: Auto-detected sync method observing TimerSettings.IsRunning
    /// Called automatically when IsRunning changes.
    /// </summary>
    private void OnTimerStateChanged()
    {
        var state = TimerSettings.IsRunning ? "STARTED" : "STOPPED";
        ObserverResults.Add($"[Internal-Sync] Timer {state} at {DateTime.Now:HH:mm:ss}");
    }

}
