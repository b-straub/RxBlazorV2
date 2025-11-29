namespace RxBlazorV2Sample.Samples.CallbackTriggers;

/// <summary>
/// Example external service that subscribes to model property changes via callback triggers.
/// This demonstrates how external services can react to model changes without direct coupling.
/// </summary>
public class CallbackTriggersService
{
    private readonly CallbackTriggersModel _model;

    public CallbackTriggersService(CallbackTriggersModel model)
    {
        _model = model;

        // Subscribe to CurrentUser changes using the generated callback method
        _model.OnCurrentUserChanged(() =>
        {
            var message = $"[Service] User changed to: {_model.CurrentUser} at {DateTime.Now:HH:mm:ss}";
            _model.CallbackResults.Add(message);
            _model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });

        // Subscribe to Settings changes using the generated async callback method
        _model.OnSettingsChangedAsync(async ct =>
        {
            var message = $"[Service] Settings updating: {_model.Settings}";
            _model.CallbackResults.Add(message);
            _model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));

            // Simulate async processing
            await Task.Delay(200, ct);

            var completeMessage = $"[Service] Settings saved at {DateTime.Now:HH:mm:ss}";
            _model.CallbackResults.Add(completeMessage);
            _model.LogEntries.Add(new Helpers.LogEntry(completeMessage, DateTime.Now));
        });

        // Subscribe using custom method name
        _model.HandleThemeUpdate(() =>
        {
            var message = $"[Service] Theme updated to: {_model.Theme} at {DateTime.Now:HH:mm:ss}";
            _model.CallbackResults.Add(message);
            _model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });

        // Subscribe to NotificationCount with both sync and async callbacks
        _model.OnNotificationCountChanged(() =>
        {
            var message = $"[Sync] Notification count: {_model.NotificationCount}";
            _model.CallbackResults.Add(message);
            _model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });

        _model.OnNotificationCountChangedAsync(async ct =>
        {
            await Task.Delay(100, ct);
            var message = $"[Async] Processed notification #{_model.NotificationCount}";
            _model.CallbackResults.Add(message);
            _model.LogEntries.Add(new Helpers.LogEntry(message, DateTime.Now));
        });
    }
}
