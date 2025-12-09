namespace RxBlazorV2Sample.Samples.CallbackTriggers;

/// <summary>
/// Example external service that subscribes to model property changes via callback triggers.
/// This demonstrates how external services can react to model changes without direct coupling.
/// </summary>
public class CallbackTriggersService
{
    public void Init(CallbackTriggersModel model)
    {
        // Subscribe to CurrentUser changes using the generated callback method
        model.OnCurrentUserChanged(() =>
        {
            var message = $"[Service] User changed to: {model.CurrentUser} at {DateTime.Now:HH:mm:ss}";
            model.CallbackResults.Add(message);
            model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });

        // Subscribe to Settings changes using the generated async callback method
        model.OnSettingsChangedAsync(async ct =>
        {
            var message = $"[Service] Settings updating: {model.Settings}";
            model.CallbackResults.Add(message);
            model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));

            // Simulate async processing
            await Task.Delay(1000, ct);

            var completeMessage = $"[Service] Settings saved at {DateTime.Now:HH:mm:ss}";
            model.CallbackResults.Add(completeMessage);
            model.LogEntries.Add(new Helpers.LogEntry(completeMessage, DateTime.Now));
        });

        // Subscribe using custom method name
        model.HandleThemeUpdate(() =>
        {
            var message = $"[Service] Theme updated to: {model.Theme} at {DateTime.Now:HH:mm:ss}";
            model.CallbackResults.Add(message);
            model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));

            switch (model.Theme)
            {
                case "Light":
                    model.Settings = "{\"theme\": \"light\"}";
                    break;
                case "Dark":
                    model.Settings = "{\"theme\": \"dark\"}";
                    break;
                case "Auto":
                    model.Settings = "{\"theme\": \"auto\"}";
                    break;
            }
        });

        // Subscribe to NotificationCount with both sync and async callbacks
        model.OnNotificationCountChanged(() =>
        {
            var message = $"[Sync] Notification count: {model.NotificationCount}";
            model.CallbackResults.Add(message);
            model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });

        model.OnNotificationCountChangedAsync(async ct =>
        {
            await Task.Delay(100, ct);
            var message = $"[Async] Processed notification #{model.NotificationCount}";
            model.CallbackResults.Add(message);
            model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });
    }
}
