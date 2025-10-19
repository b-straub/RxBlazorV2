using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// End-to-end integration tests validating the complete Observable filtering pipeline.
/// These tests simulate real-world scenarios to ensure refactorings don't break core functionality.
/// </summary>
public class SampleComponentIntegrationTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public SampleComponentIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void EndToEnd_PropertyChange_TriggersCorrectPipeline()
    {
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<SelectiveFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var pipelineEvents = new List<string>();

        // Monitor Observable emissions
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            pipelineEvents.Add($"Observable: {string.Join(", ", props)}");
        });

        // Get filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;
        pipelineEvents.Add($"Filter: [{string.Join(", ", filter)}]");

        // Act
        cut.Instance.Model.Counter = 123;
        Thread.Sleep(50);

        // Assert - verify complete pipeline
        _output.WriteLine("=== Pipeline Events ===");
        foreach (var evt in pipelineEvents)
        {
            _output.WriteLine(evt);
        }

        Assert.Contains(pipelineEvents, e => e.Contains("Filter:") && e.Contains("Model.Counter"));
        Assert.Contains(pipelineEvents, e => e.Contains("Observable:") && e.Contains("Model.Counter"));
        _output.WriteLine("✓ Complete pipeline working: Model change → Observable → Filter → Component");
    }

    [Fact]
    public void EndToEnd_ComplexScenario_MultipleComponents()
    {
        // Arrange - simulate multiple components sharing same model
        Services.AddSingleton<TestObservableModel>();

        var cut1 = Render<SelectiveFilterTestComponent>();
        var cut2 = Render<AllPropertiesFilterTestComponent>();

        cut1.WaitForState(() => cut1.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));
        cut2.WaitForState(() => cut2.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        // Both components share the same singleton model
        Assert.Same(cut1.Instance.Model, cut2.Instance.Model);

        var emissions1 = new List<string>();
        var emissions2 = new List<string>();

        using var sub1 = cut1.Instance.Model.Observable.Subscribe(props => emissions1.AddRange(props));
        using var sub2 = cut2.Instance.Model.Observable.Subscribe(props => emissions2.AddRange(props));

        // Act - change property
        cut1.Instance.Model.Counter = 42;
        Thread.Sleep(50);

        // Assert - both components receive same observable emission
        Assert.Contains("Model.Counter", emissions1);
        Assert.Contains("Model.Counter", emissions2);

        // But their filters are different
        var filter1Method = cut1.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter1 = (string[])filter1Method!.Invoke(cut1.Instance, null)!;

        var filter2Method = cut2.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter2 = (string[])filter2Method!.Invoke(cut2.Instance, null)!;

        _output.WriteLine($"Component1 filter: [{string.Join(", ", filter1)}]");
        _output.WriteLine($"Component2 filter: [{string.Join(", ", filter2)}]");

        Assert.NotEqual(filter1.Length, filter2.Length);
        _output.WriteLine("✓ Multiple components, different filters, same model emissions");
    }

    [Fact]
    public void EndToEnd_FilterOptimization_ActuallyPreventsRenders()
    {
        // This is the critical test that would have caught the "Model." prefix refactoring bug
        // Arrange
        Services.AddTransient<TestObservableModel>();
        var cut = Render<RenderCountTrackingComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        Thread.Sleep(150); // Wait for initial render
        var baselineRenderCount = cut.Instance.RenderCount;

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        _output.WriteLine($"Component filter: [{string.Join(", ", filter)}]");
        _output.WriteLine($"Baseline render count: {baselineRenderCount}");

        // Act & Assert - Test filtered property
        cut.Instance.Model.Counter = 999; // IN filter
        Thread.Sleep(150);
        var afterFilteredChange = cut.Instance.RenderCount;
        _output.WriteLine($"After filtered property (Counter): {afterFilteredChange}");
        Assert.True(afterFilteredChange > baselineRenderCount);
        _output.WriteLine("CRITICAL: Filtered property MUST trigger re-render - PASSED");

        // Act & Assert - Test non-filtered property
        cut.Instance.Model.Name = "Should Not Render"; // NOT in filter
        Thread.Sleep(150);
        var afterNonFilteredChange = cut.Instance.RenderCount;
        _output.WriteLine($"After non-filtered property (Name): {afterNonFilteredChange}");
        Assert.Equal(afterFilteredChange, afterNonFilteredChange);
        _output.WriteLine("CRITICAL: Non-filtered property MUST NOT trigger re-render (optimization) - PASSED");

        _output.WriteLine("✓ Filter optimization WORKING - this test would catch refactoring bugs!");
    }

    [Fact]
    public void EndToEnd_ObservableStreamFormat_MatchesExpectations()
    {
        // This test validates the exact format of Observable emissions
        // If refactoring changes format, this test MUST fail
        Services.AddTransient<TestObservableModel>();
        var cut = Render<TestObservableComponentWithTracking>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        string? capturedEmission = null;
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            capturedEmission = props.FirstOrDefault();
            _output.WriteLine($"Captured emission: {capturedEmission}");
        });

        // Act
        cut.Instance.Model.Counter = 777;
        Thread.Sleep(50);

        // Assert - EXACT format check
        Assert.NotNull(capturedEmission);
        Assert.Equal("Model.Counter", capturedEmission);
        _output.WriteLine("✓ Observable emission format is exactly 'Model.PropertyName'");
    }

    [Fact]
    public void EndToEnd_FilterAndEmission_MustIntersect()
    {
        // This is THE test that validates filter matching works
        // The bug we had: emissions = "TestModel.X", filter = "Model.X" → no intersection
        Services.AddTransient<TestObservableModel>();
        var cut = Render<SelectiveFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        string[]? emittedProps = null;
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProps = props;
        });

        // Act
        cut.Instance.Model.Counter = 555;
        Thread.Sleep(50);

        // Assert
        Assert.NotNull(emittedProps);
        var intersection = emittedProps.Intersect(filter).ToArray();

        _output.WriteLine($"Emitted: [{string.Join(", ", emittedProps)}]");
        _output.WriteLine($"Filter: [{string.Join(", ", filter)}]");
        _output.WriteLine($"Intersection: [{string.Join(", ", intersection)}]");

        Assert.NotEmpty(intersection);
        Assert.Contains("Model.Counter", intersection);

        _output.WriteLine("✓ CRITICAL: Filter and Observable emissions MUST intersect!");
    }

    [Fact]
    public void EndToEnd_ReferencedModel_CompleteIntegration()
    {
        // Test the complete pipeline for referenced models
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        // Get filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        string[]? emittedProps = null;
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProps = props;
            _output.WriteLine($"Parent emitted: [{string.Join(", ", props)}]");
        });

        // Act - change referenced model
        cut.Instance.Model.GetCounterModel().Counter1 = 999;
        Thread.Sleep(100);

        // Assert
        Assert.NotNull(emittedProps);

        _output.WriteLine($"Filter: [{string.Join(", ", filter)}]");
        _output.WriteLine($"Emitted: [{string.Join(", ", emittedProps)}]");

        var intersection = emittedProps.Intersect(filter).ToArray();
        Assert.NotEmpty(intersection);
        _output.WriteLine($"✓ Referenced model integration working: {string.Join(", ", intersection)}");
    }

    [Fact]
    public void EndToEnd_BatchNotifications_WorkWithFilters()
    {
        // Verify batching still works correctly with filters
        Services.AddTransient<TestObservableModel>();
        var cut = Render<SelectiveFilterTestComponent>();
        cut.WaitForState(() => cut.Instance.Model.ContextReadyCalled, timeout: TimeSpan.FromSeconds(2));

        var emissionCount = 0;
        var lastEmission = new List<string>();

        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emissionCount++;
            lastEmission = new List<string>(props);
            _output.WriteLine($"Emission {emissionCount}: [{string.Join(", ", props)}]");
        });

        // Act - batch multiple changes
        using (cut.Instance.Model.SuspendNotifications())
        {
            cut.Instance.Model.Counter = 1;
            cut.Instance.Model.Counter = 2;
            cut.Instance.Model.Counter = 3;
            cut.Instance.Model.Name = "Batched";
        }

        Thread.Sleep(50);

        // Assert - should get ONE batched emission
        Assert.Equal(1, emissionCount);
        Assert.Contains("Model.Counter", lastEmission);
        Assert.Contains("Model.Name", lastEmission);
        _output.WriteLine("✓ Batching works correctly with Observable filtering");
    }
}
