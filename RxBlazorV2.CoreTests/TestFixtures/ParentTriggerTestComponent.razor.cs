using Microsoft.AspNetCore.Components;

namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class ParentTriggerTestComponent
{
    public int RenderCount { get; private set; }
    public int ReferencedTriggerHookCallCount { get; private set; }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        RenderCount++;
    }

    protected override void OnReferencedTriggerPropertyChanged()
    {
        ReferencedTriggerHookCallCount++;
    }
}
