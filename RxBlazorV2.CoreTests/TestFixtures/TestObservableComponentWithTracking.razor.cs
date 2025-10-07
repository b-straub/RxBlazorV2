using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RxBlazorV2.Component;

namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class TestObservableComponentWithTracking : ObservableComponent<TestObservableModel>
{
    public int OnContextReadyCallCount { get; private set; }
    public int OnContextReadyAsyncCallCount { get; private set; }
    public int OnDisposeCallCount { get; private set; }
    public int OnDisposeAsyncCallCount { get; private set; }
    

    protected override void OnContextReady()
    {
        base.OnContextReady();
        OnContextReadyCallCount++;
    }

    protected override async Task OnContextReadyAsync()
    {
        await base.OnContextReadyAsync();
        OnContextReadyAsyncCallCount++;
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        OnDisposeCallCount++;
    }

    protected override ValueTask OnDisposeAsync()
    {
        OnDisposeAsyncCallCount++;
        return base.OnDisposeAsync();
    }
}
