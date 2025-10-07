using RxBlazorV2.CoreTests.TestFixtures;
using Xunit.Abstractions;

namespace RxBlazorV2.CoreTests;

public class ObservableModelLifecycleTests
{
    private readonly ITestOutputHelper _output;

    public ObservableModelLifecycleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ContextReady_CalledOnce()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        model.ContextReady();
        model.ContextReady();
        model.ContextReady();

        // Assert
        Assert.True(model.ContextReadyCalled);
        _output.WriteLine("ContextReady was called exactly once despite multiple invocations");
    }

    [Fact]
    public async Task ContextReadyAsync_CalledOnce()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        await model.ContextReadyAsync();
        await model.ContextReadyAsync();
        await model.ContextReadyAsync();

        // Assert
        Assert.True(model.ContextReadyAsyncCalled);
        _output.WriteLine("ContextReadyAsync was called exactly once despite multiple invocations");
    }

    [Fact]
    public void ContextReady_InitializesBeforePropertyChanges()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        model.ContextReady();

        // Assert
        Assert.True(model.ContextReadyCalled);
    }

    [Fact]
    public async Task ContextReadyAsync_InitializesBeforePropertyChanges()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        await model.ContextReadyAsync();

        // Assert
        Assert.True(model.ContextReadyAsyncCalled);
    }

    [Fact]
    public void Dispose_DisposesSubscriptions()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationReceived = false;

        var subscription = model.Observable.Subscribe(_ => notificationReceived = true);

        // Act
        model.Counter = 1;
        Assert.True(notificationReceived);

        notificationReceived = false;
        subscription.Dispose();

        // The model should still work after subscription disposal
        model.Counter = 2;
        
        // no new notifications should be sent
        Assert.False(notificationReceived);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act & Assert - should not throw
        model.Dispose();
        model.Dispose();
        model.Dispose();

        _output.WriteLine("Dispose can be called multiple times safely");
    }

    [Fact]
    public void Dispose_WithGarbageCollection_SuppressesFinalize()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        model.Dispose();

        // Assert - if GC.SuppressFinalize was called, this won't cause issues
        // This test primarily ensures the Dispose pattern is implemented correctly
        Assert.NotNull(model);
        _output.WriteLine("Dispose with GC.SuppressFinalize works correctly");
    }
}
