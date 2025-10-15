using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ModelReferences;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ModelReferencesModel : SampleBaseModel
{
    public override string Usage => "Models can reference other models and automatically subscribe to their changes";
    public partial string Message { get; set; } = "Welcome!";
    public partial int NotificationCount { get; set; }

    // Declare partial constructor with ModelReferencesSharedModel dependency
    public partial ModelReferencesModel(ModelReferencesSharedModel modelReferencesShared);

    [ObservableCommand(nameof(SendNotification), nameof(CanSendNotification))]
    [ObservableCommandTrigger(nameof(ModelReferencesShared.NotificationsEnabled))]
    public partial IObservableCommand SendNotificationCommand { get; }

    [ObservableCommand(nameof(UpdateMessage))]
    public partial IObservableCommand UpdateMessageCommand { get; }

    private void SendNotification()
    {
        if (ModelReferencesShared.NotificationsEnabled)
        {
            NotificationCount++;
            Message = $"Notification #{NotificationCount} sent with {ModelReferencesShared.Theme} theme at {DateTime.Now:HH:mm:ss}";
            LogEntries.Add(new LogEntry($"Notification #{NotificationCount} sent with {ModelReferencesShared.Theme} theme", DateTime.Now));
        }
    }

    private bool CanSendNotification()
    {
        return ModelReferencesShared.NotificationsEnabled;
    }

    private void UpdateMessage()
    {
        Message = $"Updated in {ModelReferencesShared.Theme} mode, Language: {ModelReferencesShared.Language}";
        LogEntries.Add(new LogEntry($"Message updated: {ModelReferencesShared.Theme} theme, {ModelReferencesShared.Language} language", DateTime.Now));
    }
}
