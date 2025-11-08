using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableComponent]
[ObservableModelScope(ModelScope.Transient)]
public partial class TriggerTestModel : ObservableModel
{
    private bool _contextReadyCalled;

    [ObservableComponentTrigger]
    public partial string SyncTriggerProperty { get; set; } = "Initial Sync";

    [ObservableComponentTriggerAsync]
    public partial int AsyncTriggerProperty { get; set; }

    [ObservableComponentTrigger(hookMethodName: "OnCustomTriggered")]
    public partial int CustomNamedTriggerProperty { get; set; }

    public partial string RegularProperty { get; set; } = "Regular";

    public bool ContextReadyCalled => _contextReadyCalled;

    protected override void OnContextReady()
    {
        _contextReadyCalled = true;
    }
}
