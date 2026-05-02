using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Tests for the <c>OnContextReadyAsync(CancellationToken)</c> overload added to
/// <c>ObservableModel</c> and <c>ObservableComponent&lt;T&gt;</c>. The token must be cancelled
/// when the model/component is disposed mid-init, and the parameterless overload must remain
/// usable for backwards compatibility.
/// </summary>
public class ContextReadyCancellationTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ContextReadyCancellationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Model_OnContextReadyAsync_WithCancellation_TokenIsCancelledOnDispose()
    {
        // Arrange
        var model = new CancellationAwareModel { DelayDuration = TimeSpan.FromSeconds(5) };

        // Act — start init, wait until the override is awaiting Task.Delay, then dispose.
        var initTask = model.ContextReadyAsync();
        await model.EnteredAsync.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        model.Dispose();

        // Awaiting the init task must NOT throw — ContextReadyAsync swallows the disposal-triggered OCE.
        await initTask;

        // Assert
        Assert.True(model.TokenWasCancelled, "Override must observe OperationCanceledException");
        Assert.False(model.DelayCompleted, "Post-delay code must not execute after disposal");
        _output.WriteLine("Model disposal cancels the OnContextReadyAsync token");
    }

    [Fact]
    public async Task Model_OnContextReadyAsync_WithCancellation_HappyPathCompletesWithoutCancel()
    {
        // Arrange
        var model = new CancellationAwareModel { DelayDuration = TimeSpan.FromMilliseconds(50) };

        // Act
        await model.ContextReadyAsync();

        // Assert
        Assert.True(model.DelayCompleted, "Delay must complete on the happy path");
        Assert.False(model.TokenWasCancelled, "Token must NOT be cancelled when not disposed");
        _output.WriteLine("Happy path completes without observing cancellation");
    }

    [Fact]
    public async Task Model_OnContextReadyAsync_LegacyParameterlessOverride_StillInvoked()
    {
        // Backwards compatibility: a model that overrides only the parameterless OnContextReadyAsync()
        // must continue to work. TestObservableModel uses that legacy signature.
        var model = new TestObservableModel();

        await model.ContextReadyAsync();

        Assert.True(model.ContextReadyAsyncCalled, "Legacy parameterless override must still be called");
        _output.WriteLine("Legacy parameterless OnContextReadyAsync override still invoked");
    }

    [Fact]
    public async Task Component_OnContextReadyAsync_WithCancellation_TokenIsCancelledOnDispose()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<CancellationAwareComponent>();

        // Capture the instance reference before dispose — bunit invalidates cut.Instance afterwards.
        var instance = cut.Instance;

        // Wait until the override has entered Task.Delay.
        await instance.EnteredAsync.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);

        // Act — invoke Dispose on the renderer's dispatcher so the cancellation continuation
        // (which runs on the same dispatcher) actually gets to flip TokenWasCancelled.
        await cut.InvokeAsync(() => ((IDisposable)instance).Dispose());

        // Give the cancellation a moment to propagate through the awaiter.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!instance.TokenWasCancelled && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Xunit.TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.True(instance.TokenWasCancelled, "Component override must observe OperationCanceledException");
        Assert.False(instance.DelayCompleted, "Post-delay code must not execute after disposal");
        _output.WriteLine("Component disposal cancels the OnContextReadyAsync token");
    }

    [Fact]
    public void Component_OnContextReadyAsync_LegacyParameterlessOverride_StillInvoked()
    {
        // TestObservableComponentWithTracking overrides the legacy parameterless signature.
        Services.AddTransient<TestObservableModel>();

        var cut = Render<TestObservableComponentWithTracking>();
        cut.WaitForState(() => cut.Instance.OnContextReadyAsyncCallCount > 0, timeout: TimeSpan.FromSeconds(2));

        Assert.Equal(1, cut.Instance.OnContextReadyAsyncCallCount);
        _output.WriteLine("Legacy parameterless component override still invoked");
    }
}
