using ObservableCollections;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.CallbackTriggers;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class CallbackTriggersModel : SampleBaseModel
{
    public override string Usage => "Callback triggers allow external services to subscribe to property changes";

    // Sync callback trigger - generates OnCurrentUserChanged(Action callback)
    [ObservableCallbackTrigger]
    public partial string CurrentUser { get; set; } = "Guest";

    // Async callback trigger - generates OnSettingsChangedAsync(Func<CancellationToken, Task> callback)
    [ObservableCallbackTriggerAsync]
    public partial string Settings { get; set; } = "{}";

    // Custom method name
    [ObservableCallbackTrigger("HandleThemeUpdate")]
    public partial string Theme { get; set; } = "Light";

    // Both sync and async on same property
    [ObservableCallbackTrigger]
    [ObservableCallbackTriggerAsync]
    public partial int NotificationCount { get; set; }

    // For displaying callback execution results
    public partial ObservableList<string> CallbackResults { get; init; } = [];
}
