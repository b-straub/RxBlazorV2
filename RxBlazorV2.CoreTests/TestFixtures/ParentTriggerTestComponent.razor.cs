namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class ParentTriggerTestComponent
{
    public int RenderCount { get; private set; }
    public int ReferencedTriggerHookCallCount { get; private set; }

    protected override void OnReferencedTriggerPropertyChanged()
    {
        ReferencedTriggerHookCallCount++;
    }
}
