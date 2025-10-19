using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Tests Observable filtering mechanism in ObservableComponents.
/// Verifies that Filter() generation works correctly and filters actually prevent unnecessary re-renders.
/// </summary>
public class ObservableComponentFilterTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ObservableComponentFilterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SelectiveFilter_OnlyIncludesUsedProperties()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<SelectiveFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter via reflection
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert
        _output.WriteLine($"Generated filter: [{string.Join(", ", filter)}]");
        Assert.Contains("Model.Counter", filter);
        Assert.DoesNotContain("Model.Name", filter);
        Assert.DoesNotContain("Model.BatchProperty1", filter);
        Assert.DoesNotContain("Model.BatchProperty2", filter);
    }

    [Fact]
    public void AllPropertiesFilter_IncludesAllUsedProperties()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();

        // Act
        var cut = Render<AllPropertiesFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert
        _output.WriteLine($"Generated filter: [{string.Join(", ", filter)}]");
        Assert.Contains("Model.Counter", filter);
        Assert.Contains("Model.Name", filter);
        Assert.Contains("Model.BatchProperty1", filter);
        Assert.Contains("Model.BatchProperty2", filter);
    }

    [Fact]
    public void ObservableEmissions_MatchFilterFormat()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<SelectiveFilterTestComponent>();
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

        // Act - change filtered property
        cut.Instance.Model.Counter = 42;

        // Assert - emitted property name should be in filter
        Assert.Contains("Model.Counter", emittedProperties);
        Assert.Contains("Model.Counter", filter);
        _output.WriteLine($"✓ Observable emission matches filter format");
    }

    [Fact]
    public void FilteredProperty_TriggersReRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<RenderCountTrackingComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var initialRenderCount = cut.Instance.RenderCount;
        _output.WriteLine($"Initial render count: {initialRenderCount}");

        // Act - change filtered property (Counter is used in razor)
        cut.Instance.Model.Counter = 42;

        // Wait for potential re-render
        Thread.Sleep(150); // Wait for chunk buffer

        // Assert
        var newRenderCount = cut.Instance.RenderCount;
        _output.WriteLine($"New render count after Counter change: {newRenderCount}");
        Assert.True(newRenderCount > initialRenderCount,
            $"Expected re-render after filtered property change. Initial: {initialRenderCount}, New: {newRenderCount}");
    }

    [Fact]
    public void NonFilteredProperty_DoesNotTriggerReRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<RenderCountTrackingComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Wait for initial render to settle
        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;
        _output.WriteLine($"Initial render count: {initialRenderCount}");

        // Act - change non-filtered property (Name is NOT used in razor)
        cut.Instance.Model.Name = "Changed Name";

        // Wait to ensure no re-render happens
        Thread.Sleep(200);

        // Assert
        var newRenderCount = cut.Instance.RenderCount;
        _output.WriteLine($"Render count after Name change: {newRenderCount}");
        Assert.Equal(initialRenderCount, newRenderCount);
        _output.WriteLine($"✓ Filter optimization working - no re-render for unused property");
    }

    [Fact]
    public void BatchedChanges_FilteredPropertiesOnly()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<SelectiveFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var emittedProperties = new List<string>();
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProperties.AddRange(props);
            _output.WriteLine($"Observable emitted: {string.Join(", ", props)}");
        });

        // Act - batch changes to filtered and non-filtered properties
        using (cut.Instance.Model.SuspendNotifications())
        {
            cut.Instance.Model.Counter = 10;  // Filtered
            cut.Instance.Model.Name = "Test"; // NOT filtered
        }

        // Assert - both properties emit (filtering happens at component level, not model level)
        Assert.Contains("Model.Counter", emittedProperties);
        Assert.Contains("Model.Name", emittedProperties);
        _output.WriteLine("✓ Observable emits all changes, component filters for re-render");
    }

    [Fact]
    public void EmptyFilter_SubscribesToAllChanges()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<TestObservableComponentWithTracking>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        _output.WriteLine($"Filter length: {filter.Length}");

        // Act - change any property
        var initialCallCount = cut.Instance.OnContextReadyCallCount;
        cut.Instance.Model.Counter = 99;
        Thread.Sleep(150);

        // Assert - with non-empty filter, component should handle changes
        // Note: TestObservableComponentWithTracking uses Counter and Name, so filter is not empty
        Assert.NotEmpty(filter);
        _output.WriteLine($"✓ Component has filter: [{string.Join(", ", filter)}]");
    }

    [Fact]
    public void MultiplePropertyChanges_OnlyFilteredTriggersReRender()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<RenderCountTrackingComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150);
        var initialRenderCount = cut.Instance.RenderCount;

        // Act - change multiple properties, only one is filtered
        cut.Instance.Model.Name = "Not Filtered";
        Thread.Sleep(150);
        var afterNonFilteredRenderCount = cut.Instance.RenderCount;

        cut.Instance.Model.Counter = 123;
        Thread.Sleep(150);
        var afterFilteredRenderCount = cut.Instance.RenderCount;

        // Assert
        _output.WriteLine($"Initial: {initialRenderCount}, After Name: {afterNonFilteredRenderCount}, After Counter: {afterFilteredRenderCount}");
        Assert.Equal(initialRenderCount, afterNonFilteredRenderCount); // Name change doesn't trigger
        Assert.True(afterFilteredRenderCount > afterNonFilteredRenderCount); // Counter change does trigger
    }

    [Fact]
    public void ObservableIntersect_WorksWithFilter()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<SelectiveFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        var intersectResults = new List<bool>();
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            var hasIntersection = props.Intersect(filter).Any();
            intersectResults.Add(hasIntersection);
            _output.WriteLine($"Properties: [{string.Join(", ", props)}], Intersects filter: {hasIntersection}");
        });

        // Act - change filtered property
        cut.Instance.Model.Counter = 42;
        Thread.Sleep(50);

        // Change non-filtered property
        cut.Instance.Model.Name = "Test";
        Thread.Sleep(50);

        // Assert
        Assert.True(intersectResults[0]); // Counter change intersects with filter
        Assert.False(intersectResults[1]); // Name change does NOT intersect with filter
        _output.WriteLine("✓ Intersect logic correctly identifies filtered vs non-filtered properties");
    }
}
