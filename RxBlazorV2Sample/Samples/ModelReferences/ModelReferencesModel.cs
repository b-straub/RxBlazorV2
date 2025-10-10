using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ModelReferences;

[ObservableModelReference<ModelReferencesSharedModel>]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ModelReferencesModel : SampleBaseModel
{
    public override string Usage => "Models can reference other models and automatically subscribe to their changes";
    public partial string Message { get; set; } = "Welcome!";
    public partial int NotificationCount { get; set; }

    [ObservableCommand(nameof(SendNotification), nameof(CanSendNotification))]
    [ObservableCommandTrigger(nameof(ModelReferencesSharedModel.NotificationsEnabled))]
    public partial IObservableCommand SendNotificationCommand { get; }

    [ObservableCommand(nameof(UpdateMessage))]
    public partial IObservableCommand UpdateMessageCommand { get; }

    private void SendNotification()
    {
        if (ModelReferencesSharedModel.NotificationsEnabled)
        {
            NotificationCount++;
            Message = $"Notification #{NotificationCount} sent with {ModelReferencesSharedModel.Theme} theme at {DateTime.Now:HH:mm:ss}";
            LogEntries.Add(new LogEntry($"Notification #{NotificationCount} sent with {ModelReferencesSharedModel.Theme} theme", DateTime.Now));
        }
    }

    private bool CanSendNotification()
    {
        return ModelReferencesSharedModel.NotificationsEnabled;
    }

    private void UpdateMessage()
    {
        Message = $"Updated in {ModelReferencesSharedModel.Theme} mode, Language: {ModelReferencesSharedModel.Language}";
        LogEntries.Add(new LogEntry($"Message updated: {ModelReferencesSharedModel.Theme} theme, {ModelReferencesSharedModel.Language} language", DateTime.Now));
    }
}
