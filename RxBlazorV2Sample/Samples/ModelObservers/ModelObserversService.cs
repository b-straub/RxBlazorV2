using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ModelObservers;

/// <summary>
/// Example external service that subscribes to model property changes via [ObservableModelObserver].
/// This demonstrates how external services can react to model changes without direct coupling.
///
/// External observers require the [ObservableModelObserver] attribute, unlike internal model
/// methods which are auto-detected by the generator.
/// </summary>
public class ModelObserversService
{
    /// <summary>
    /// External observer: Sync method triggered when CurrentUser changes
    /// </summary>
    [ObservableModelObserver(nameof(ModelObserversModel.CurrentUser))]
    public void CurrentUserChanged(ModelObserversModel model)
    {
        var message = $"[External-Sync] User changed to: {model.CurrentUser} at {DateTime.Now:HH:mm:ss}";
        model.ObserverResults.Add(message);
        model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
    }

    /// <summary>
    /// External observer: Async method triggered when Settings changes
    /// </summary>
    [ObservableModelObserver(nameof(ModelObserversModel.Settings))]
    public async Task SettingsChangedAsync(ModelObserversModel model)
    {
        var message = $"[External-Async] Settings updating: {model.Settings}";
        model.ObserverResults.Add(message);
        model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));

        // Simulate async processing
        await Task.Delay(1000);

        var completeMessage = $"[External-Async] Settings saved at {DateTime.Now:HH:mm:ss}";
        model.ObserverResults.Add(completeMessage);
        model.LogEntries.Add(new Helpers.LogEntry(completeMessage, DateTime.Now));
    }

    /// <summary>
    /// External observer: Sync method with custom name, triggered when Theme changes
    /// </summary>
    [ObservableModelObserver(nameof(ModelObserversModel.Theme))]
    public void HandleThemeUpdate(ModelObserversModel model)
    {
        var message = $"[External-Sync] Theme updated to: {model.Theme} at {DateTime.Now:HH:mm:ss}";
        model.ObserverResults.Add(message);
        model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));

        model.Settings = model.Theme switch
        {
            "Light" => "{\"theme\": \"light\"}",
            "Dark" => "{\"theme\": \"dark\"}",
            "Auto" => "{\"theme\": \"auto\"}",
            _ => model.Settings
        };
    }

    /// <summary>
    /// External observer: Sync method triggered when NotificationCount changes
    /// </summary>
    [ObservableModelObserver(nameof(ModelObserversModel.NotificationCount))]
    public void NotificationCountChanged(ModelObserversModel model)
    {
        var message = $"[External-Sync] Notification count: {model.NotificationCount}";
        model.ObserverResults.Add(message);
        model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
    }

    /// <summary>
    /// External observer: Async method with CancellationToken, triggered when NotificationCount changes
    /// </summary>
    [ObservableModelObserver(nameof(ModelObserversModel.NotificationCount))]
    public async Task NotificationCountChangedAsync(ModelObserversModel model, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        var message = $"[External-Async+CT] Processed notification #{model.NotificationCount}";
        model.ObserverResults.Add(message);
        model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
    }

    /// <summary>
    /// External observer: Single method observing multiple properties (CurrentUser AND Theme)
    /// </summary>
    [ObservableModelObserver(nameof(ModelObserversModel.CurrentUser))]
    [ObservableModelObserver(nameof(ModelObserversModel.Theme))]
    public void CombinedObserver(ModelObserversModel model)
    {
        var message = $"[External-Combined] User: {model.CurrentUser}, Theme: {model.Theme} at {DateTime.Now:HH:mm:ss}";
        model.ObserverResults.Add(message);
        model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
    }
}
