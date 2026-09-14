using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RxBlazorV2.CoreTests.TestFixtures;

/// <summary>
/// Hosts <see cref="DerivedPhaseComponent"/> and flips <see cref="BootStateModel"/> in its own
/// OnAfterRender. Render-completed callbacks run parent first, so this executes after the child's
/// first render but before the child's OnAfterRender — i.e. before the child has subscribed to its
/// model. Mirrors a layout that starts a service during boot.
/// Written in C# because a razor file cannot compose a generated component of the same assembly
/// (RXBG061 / RZ10012).
/// </summary>
public class EarlyEmitHost : ComponentBase
{
    [Inject]
    public required BootStateModel Boot { get; init; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DerivedPhaseComponent>(0);
        builder.CloseComponent();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Boot.State = BootPhase.Initializing;
        }
    }
}
