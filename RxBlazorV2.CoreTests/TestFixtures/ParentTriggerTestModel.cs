using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableComponent]
[ObservableModelScope(ModelScope.Transient)]
public partial class ParentTriggerTestModel : ObservableModel
{
    private bool _contextReadyCalled;

    public partial ParentTriggerTestModel(ReferencedTriggerTestModel referenced);

    public partial string ParentProperty { get; set; } = "Parent";

    public bool ContextReadyCalled => _contextReadyCalled;

    protected override void OnContextReady()
    {
        _contextReadyCalled = true;
    }
}
