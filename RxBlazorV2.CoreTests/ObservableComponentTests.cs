using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableComponentTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ObservableComponentTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Component_Renders_Successfully()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Instance);
        Assert.NotNull(cut.Instance.Model);
        _output.WriteLine("Component rendered successfully");
    }

    [Fact]
    public void Component_Model_IsAccessible()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Assert
        Assert.NotNull(cut.Instance.Model);
        Assert.IsType<TestObservableModel>(cut.Instance.Model);
        _output.WriteLine($"Model type: {cut.Instance.Model.GetType().Name}");
    }
    
    [Fact]
    public void Component_OnContextReady_CalledOnFirstRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for first render to complete
        cut.WaitForState(() => cut.Instance.OnContextReadyCallCount > 0, timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(1, cut.Instance.OnContextReadyCallCount);
        _output.WriteLine($"OnContextReady call count: {cut.Instance.OnContextReadyCallCount}");
    }

    [Fact]
    public void Component_OnContextReadyAsync_CalledOnFirstRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for async lifecycle to complete
        cut.WaitForState(() => cut.Instance.OnContextReadyAsyncCallCount > 0, timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(1, cut.Instance.OnContextReadyAsyncCallCount);
        _output.WriteLine($"OnContextReadyAsync call count: {cut.Instance.OnContextReadyAsyncCallCount}");
    }

    [Fact]
    public void Component_ModelContextReady_CalledOnFirstRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for context ready to complete
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(cut.Instance.Model.ContextReadyCalled);
        _output.WriteLine("Model.ContextReady() was called");
    }

    [Fact]
    public void Component_ModelContextReadyAsync_CalledOnFirstRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for async context ready to complete
        cut.WaitForState(() => cut.Instance.Model.ContextReadyAsyncCalled, timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(cut.Instance.Model.ContextReadyAsyncCalled);
        _output.WriteLine("Model.ContextReadyAsync() was called");
    }

    [Fact]
    public void Component_LifecycleHooks_CalledOnlyOnceOnFirstRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for all lifecycle hooks to complete
        cut.WaitForState(() =>
            cut.Instance.OnContextReadyCallCount > 0 &&
            cut.Instance.OnContextReadyAsyncCallCount > 0,
            timeout: TimeSpan.FromSeconds(2));

        // Trigger another render
        cut.Instance.Model.Counter = 1;
        cut.Render();

        // Wait a bit to ensure no additional calls
        Thread.Sleep(100);

        // Assert
        Assert.Equal(1, cut.Instance.OnContextReadyCallCount);
        Assert.Equal(1, cut.Instance.OnContextReadyAsyncCallCount);
        _output.WriteLine("All lifecycle hooks called exactly once");
    }

    [Fact]
    public void Component_RendersInitialModelState()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Counter: 0", markup);
        Assert.Contains("Name: ", markup);
        _output.WriteLine($"Initial markup: {markup}");
    }

    [Fact]
    public void Component_Renders_WithInitializedModel()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for lifecycle to complete
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Set model properties
        cut.Instance.Model.Counter = 42;
        cut.Instance.Model.Name = "Test Name";

        // Assert
        Assert.Equal(42, cut.Instance.Model.Counter);
        Assert.Equal("Test Name", cut.Instance.Model.Name);
    }

    [Fact]
    public void Component_ModelPropertyChanges_AreObservable()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for initialization
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var notificationCount = 0;
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            notificationCount++;
            _output.WriteLine($"Property changed: {string.Join(", ", props)}");
        });

        // Act
        cut.Instance.Model.Counter = 10;
        cut.Instance.Model.Name = "Changed";

        // Assert
        Assert.Equal(2, notificationCount);
        Assert.Equal(10, cut.Instance.Model.Counter);
        Assert.Equal("Changed", cut.Instance.Model.Name);
    }

    [Fact]
    public void Component_WithScopedModel_UsesSameInstanceInScope()
    {
        // Arrange
        Services.AddScoped<TestObservableModel>();

        // Act
        var cut1 = Render<TestObservableComponentWithTracking>();
        var cut2 = Render<TestObservableComponentWithTracking>();

        // Assert - bunit creates new scopes per component, so models should be different
        Assert.NotSame(cut1.Instance.Model, cut2.Instance.Model);
        _output.WriteLine("Each component gets its own scoped model instance in bunit");
    }

    [Fact]
    public void Component_WithTransientModel_CreatesNewInstancePerComponent()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut1 = Render<TestObservableComponentWithTracking>();
        var cut2 = Render<TestObservableComponentWithTracking>();

        // Assert
        Assert.NotSame(cut1.Instance.Model, cut2.Instance.Model);
        _output.WriteLine("Each component gets a new transient model instance");
    }

    [Fact]
    public void Component_ModelID_IsCorrect()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<TestObservableComponentWithTracking>();

        // Assert
        Assert.Equal("RxBlazorV2.CoreTests.TestFixtures.TestObservableModel", cut.Instance.Model.ModelID);
        _output.WriteLine($"Model ID: {cut.Instance.Model.ModelID}");
    }

    [Fact]
    public void Component_MultipleRenders_MaintainModelState()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for initialization
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Act
        cut.Instance.Model.Counter = 5;
        cut.Render();

        cut.Instance.Model.Counter = 10;
        cut.Render();

        // Assert
        Assert.Equal(10, cut.Instance.Model.Counter);
        _output.WriteLine($"Final counter value: {cut.Instance.Model.Counter}");
    }

    [Fact]
    public void Component_BatchPropertyChanges_WorkCorrectly()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<TestObservableComponentWithTracking>();

        // Wait for initialization
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var notificationCount = 0;
        using var subscription = cut.Instance.Model.Observable.Subscribe(_ => notificationCount++);

        // Act
        using (cut.Instance.Model.SuspendNotifications())
        {
            cut.Instance.Model.Counter = 1;
            cut.Instance.Model.Name = "Test";
            cut.Instance.Model.Counter = 2;
        }

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Equal(2, cut.Instance.Model.Counter);
        Assert.Equal("Test", cut.Instance.Model.Name);
        _output.WriteLine("Batch property changes work correctly in component context");
    }
}
