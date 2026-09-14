using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// A model change that happens between a component's first render and its OnAfterRender
/// must still reach the view. The generated Model.Observable subscription is created in
/// OnAfterRender(firstRender), so an emission in that window currently has no subscriber:
/// the model moves on, the setter's equality guard blocks any later identical emission,
/// and the component keeps showing its first-render branch.
/// </summary>
public class ObservableComponentEarlyEmissionTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ObservableComponentEarlyEmissionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ModelChangeBetweenFirstRenderAndOnAfterRender_ReRendersComponent()
    {
        // Arrange
        Services.AddSingleton<BootStateModel>();
        Services.AddTransient<DerivedPhaseModel>();

        // Act - the host flips BootStateModel in its OnAfterRender, which runs before the
        // child's OnAfterRender (and therefore before the child subscribed to its model)
        var cut = Render<EarlyEmitHost>();
        var page = cut.FindComponent<DerivedPhaseComponent>();

        _output.WriteLine($"After Render: Model.Phase={page.Instance.Model.Phase}, markup={page.Find("#phase").TextContent}, renders={page.Instance.RenderCount}");

        // Assert - the observer ran and the model moved on...
        Assert.Equal(DerivedPhase.Waiting, page.Instance.Model.Phase);

        // ...and the view has to follow (Chunk window is 100 ms)
        page.WaitForAssertion(
            () => Assert.Equal("Waiting", page.Find("#phase").TextContent),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ModelChangeAfterOnAfterRender_ReRendersComponent()
    {
        // Arrange
        Services.AddSingleton<BootStateModel>();
        Services.AddTransient<DerivedPhaseModel>();
        var cut = Render<EarlyEmitHost>();
        var page = cut.FindComponent<DerivedPhaseComponent>();
        var boot = Services.GetRequiredService<BootStateModel>();

        // Act - a second transition happens after the child subscribed
        boot.State = BootPhase.Ready;

        // Assert - this one is rendered, which shows only the first transition is lost
        Assert.Equal(DerivedPhase.Ready, page.Instance.Model.Phase);
        page.WaitForAssertion(
            () => Assert.Equal("Ready", page.Find("#phase").TextContent),
            TimeSpan.FromSeconds(1));
    }
}
