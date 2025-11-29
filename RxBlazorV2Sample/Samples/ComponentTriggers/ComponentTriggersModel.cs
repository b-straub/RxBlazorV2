using ObservableCollections;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ComponentTriggers;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ComponentTriggersModel : SampleBaseModel
{
    public override string Usage => "Component triggers generate hook methods that execute when specific properties change";

    // RenderAndHook (default) - generates hook AND triggers re-render
    [ObservableComponentTrigger]
    public partial string Theme { get; set; } = "Light";

    // Async hook with custom name
    [ObservableComponentTriggerAsync(hookMethodName: "HandleUserNameUpdateAsync")]
    public partial string UserName { get; set; } = "";

    // RenderOnly - triggers re-render but no hook method
    [ObservableComponentTrigger(ComponentTriggerType.RenderOnly)]
    public partial int RenderOnlyCounter { get; set; }

    // HookOnly - calls hook but no automatic re-render
    [ObservableComponentTrigger(ComponentTriggerType.HookOnly)]
    public partial string BackgroundStatus { get; set; } = "Idle";

    // Track hook executions for demonstration
    public partial int ThemeHookCount { get; set; }
    public partial int UserNameHookCount { get; set; }
    public partial int BackgroundHookCount { get; set; }
    public partial string LastHookMessage { get; set; } = "No hooks executed yet";
}
