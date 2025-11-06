using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Tests that properties with [ObservableComponentTrigger] are automatically included in the Filter()
/// to ensure both hook execution AND component re-rendering.
/// Fixes: Component trigger hooks were called but components didn't re-render because trigger properties
/// were not included in the auto-generated Filter() method.
/// </summary>
public class ObservableComponentTriggerRerenderTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ObservableComponentTriggerRerenderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LocalTrigger_PropertyIncludedInFilter()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();

        // Act
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter via reflection
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert
        _output.WriteLine($"Generated filter: [{string.Join(", ", filter)}]");
        Assert.Contains("Model.SyncTriggerProperty", filter);
        Assert.Contains("Model.AsyncTriggerProperty", filter);
    }

    [Fact]
    public void LocalSyncTrigger_BothHookAndReRenderOccur()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;
        var initialHookCallCount = cut.Instance.SyncTriggerHookCallCount;

        _output.WriteLine($"Initial - Renders: {initialRenderCount}, Hook calls: {initialHookCallCount}");

        // Act - change property with sync trigger
        cut.Instance.Model.SyncTriggerProperty = "Changed";

        // Wait for chunk buffer and hook execution
        Thread.Sleep(150);

        // Assert - both hook AND re-render should occur
        var newRenderCount = cut.Instance.RenderCount;
        var newHookCallCount = cut.Instance.SyncTriggerHookCallCount;

        _output.WriteLine($"After change - Renders: {newRenderCount}, Hook calls: {newHookCallCount}");

        Assert.True(newHookCallCount > initialHookCallCount,
            $"Hook should be called. Initial: {initialHookCallCount}, New: {newHookCallCount}");
        Assert.True(newRenderCount > initialRenderCount,
            $"Component should re-render. Initial: {initialRenderCount}, New: {newRenderCount}");

        _output.WriteLine("✓ Sync trigger property change triggers both hook AND re-render");
    }

    [Fact]
    public async Task LocalAsyncTrigger_BothHookAndReRenderOccur()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;
        var initialHookCallCount = cut.Instance.AsyncTriggerHookCallCount;

        _output.WriteLine($"Initial - Renders: {initialRenderCount}, Hook calls: {initialHookCallCount}");

        // Act - change property with async trigger
        cut.Instance.Model.AsyncTriggerProperty = 42;

        // Wait for chunk buffer and async hook execution
        await Task.Delay(200);

        // Assert - both hook AND re-render should occur
        var newRenderCount = cut.Instance.RenderCount;
        var newHookCallCount = cut.Instance.AsyncTriggerHookCallCount;

        _output.WriteLine($"After change - Renders: {newRenderCount}, Hook calls: {newHookCallCount}");

        Assert.True(newHookCallCount > initialHookCallCount,
            $"Async hook should be called. Initial: {initialHookCallCount}, New: {newHookCallCount}");
        Assert.True(newRenderCount > initialRenderCount,
            $"Component should re-render. Initial: {initialRenderCount}, New: {newRenderCount}");

        _output.WriteLine("✓ Async trigger property change triggers both hook AND re-render");
    }

    [Fact]
    public void ReferencedModelTrigger_PropertyIncludedInFilter()
    {
        // Arrange
        Services.AddSingleton<ReferencedTriggerTestModel>();
        Services.AddTransient<ParentTriggerTestModel>();

        // Act
        var cut = Render<ParentTriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter via reflection
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert
        _output.WriteLine($"Generated filter: [{string.Join(", ", filter)}]");
        Assert.Contains("Model.Referenced.TriggerProperty", filter);
    }

    [Fact]
    public void ReferencedModelTrigger_BothHookAndReRenderOccur()
    {
        // Arrange
        Services.AddSingleton<ReferencedTriggerTestModel>();
        Services.AddTransient<ParentTriggerTestModel>();

        var cut = Render<ParentTriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;
        var initialHookCallCount = cut.Instance.ReferencedTriggerHookCallCount;

        _output.WriteLine($"Initial - Renders: {initialRenderCount}, Hook calls: {initialHookCallCount}");

        // Act - change property in referenced model with trigger
        cut.Instance.Model.Referenced.TriggerProperty = "Changed from parent";

        // Wait for chunk buffer and hook execution
        Thread.Sleep(150);

        // Assert - both hook AND re-render should occur
        var newRenderCount = cut.Instance.RenderCount;
        var newHookCallCount = cut.Instance.ReferencedTriggerHookCallCount;

        _output.WriteLine($"After change - Renders: {newRenderCount}, Hook calls: {newHookCallCount}");

        Assert.True(newHookCallCount > initialHookCallCount,
            $"Referenced model hook should be called. Initial: {initialHookCallCount}, New: {newHookCallCount}");
        Assert.True(newRenderCount > initialRenderCount,
            $"Parent component should re-render. Initial: {initialRenderCount}, New: {newRenderCount}");

        _output.WriteLine("✓ Referenced model trigger property change triggers both hook AND re-render in parent");
    }

    [Fact]
    public void MultipleTriggers_AllIncludedInFilter()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();

        // Act
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert - all trigger properties should be in filter
        _output.WriteLine($"Generated filter: [{string.Join(", ", filter)}]");
        Assert.Contains("Model.SyncTriggerProperty", filter);
        Assert.Contains("Model.AsyncTriggerProperty", filter);
        Assert.Contains("Model.CustomNamedTriggerProperty", filter);
    }

    [Fact]
    public void TriggerWithCustomName_BothHookAndReRenderOccur()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;
        var initialHookCallCount = cut.Instance.CustomNamedHookCallCount;

        _output.WriteLine($"Initial - Renders: {initialRenderCount}, Hook calls: {initialHookCallCount}");

        // Act - change property with custom-named trigger
        cut.Instance.Model.CustomNamedTriggerProperty = 999;

        // Wait for chunk buffer and hook execution
        Thread.Sleep(150);

        // Assert - both hook AND re-render should occur
        var newRenderCount = cut.Instance.RenderCount;
        var newHookCallCount = cut.Instance.CustomNamedHookCallCount;

        _output.WriteLine($"After change - Renders: {newRenderCount}, Hook calls: {newHookCallCount}");

        Assert.True(newHookCallCount > initialHookCallCount,
            $"Custom-named hook should be called. Initial: {initialHookCallCount}, New: {newHookCallCount}");
        Assert.True(newRenderCount > initialRenderCount,
            $"Component should re-render. Initial: {initialRenderCount}, New: {newRenderCount}");

        _output.WriteLine("✓ Custom-named trigger property change triggers both hook AND re-render");
    }

    [Fact]
    public void NonTriggerProperty_OnlyReRendersNoHook()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;
        var initialSyncHookCallCount = cut.Instance.SyncTriggerHookCallCount;
        var initialAsyncHookCallCount = cut.Instance.AsyncTriggerHookCallCount;

        _output.WriteLine($"Initial - Renders: {initialRenderCount}");

        // Act - change regular property (not a trigger, but used in razor)
        cut.Instance.Model.RegularProperty = "Changed Regular";

        // Wait for chunk buffer
        Thread.Sleep(150);

        // Assert - re-render occurs but NO hooks are called
        var newRenderCount = cut.Instance.RenderCount;
        var newSyncHookCallCount = cut.Instance.SyncTriggerHookCallCount;
        var newAsyncHookCallCount = cut.Instance.AsyncTriggerHookCallCount;

        _output.WriteLine($"After change - Renders: {newRenderCount}");

        Assert.True(newRenderCount > initialRenderCount,
            $"Component should re-render for regular property. Initial: {initialRenderCount}, New: {newRenderCount}");
        Assert.Equal(initialSyncHookCallCount, newSyncHookCallCount);
        Assert.Equal(initialAsyncHookCallCount, newAsyncHookCallCount);

        _output.WriteLine("✓ Regular property triggers re-render but not hooks");
    }

    [Fact]
    public void ObservableEmission_IncludesTriggerPropertyInCorrectFormat()
    {
        // Arrange
        Services.AddTransient<TriggerTestModel>();
        var cut = Render<TriggerTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var emittedProperties = new List<string>();
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProperties.AddRange(props);
            _output.WriteLine($"Observable emitted: {string.Join(", ", props)}");
        });

        // Get the generated filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Act - change trigger property
        cut.Instance.Model.SyncTriggerProperty = "Observable Test";
        Thread.Sleep(50);

        // Assert - emitted property name should be in filter
        Assert.Contains("Model.SyncTriggerProperty", emittedProperties);
        Assert.Contains("Model.SyncTriggerProperty", filter);
        _output.WriteLine($"✓ Trigger property emission matches filter format");
    }
}
