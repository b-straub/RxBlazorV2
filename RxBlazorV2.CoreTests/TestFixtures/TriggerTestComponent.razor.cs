using Microsoft.AspNetCore.Components;

namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class TriggerTestComponent
{
    public int RenderCount { get; private set; }
    public int SyncTriggerHookCallCount { get; private set; }
    public int AsyncTriggerHookCallCount { get; private set; }
    public int CustomNamedHookCallCount { get; private set; }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        RenderCount++;
    }

    protected override void OnSyncTriggerPropertyChanged()
    {
        SyncTriggerHookCallCount++;
    }

    protected override Task OnAsyncTriggerPropertyChangedAsync(CancellationToken ct)
    {
        AsyncTriggerHookCallCount++;
        return Task.CompletedTask;
    }

    protected override void OnCustomTriggered()
    {
        CustomNamedHookCallCount++;
    }
}
