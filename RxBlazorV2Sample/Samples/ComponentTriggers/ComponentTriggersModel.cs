using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ComponentTriggers;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ComponentTriggersModel : SampleBaseModel
{
    public override string Usage => "Component triggers generate hook methods that execute when specific properties change";

    // Generates OnThemeChanged() hook
    [ObservableComponentTrigger]
    public partial string Theme { get; set; } = "Light";

    // Async hook with custom name
    [ObservableComponentTriggerAsync("HandleUserNameUpdateAsync")]
    public partial string UserName { get; set; } = "";

    // No trigger needed - property in razor handles rendering
    public partial int RenderOnlyCounter { get; set; }

    // Generates OnBackgroundStatusChanged() hook
    [ObservableComponentTrigger]
    public partial string BackgroundStatus { get; set; } = "Idle";

    // Track hook executions for demonstration
    public partial int ThemeHookCount { get; set; }
    public partial int UserNameHookCount { get; set; }
    public partial int BackgroundHookCount { get; set; }
    public partial string LastHookMessage { get; set; } = "No hooks executed yet";
}
