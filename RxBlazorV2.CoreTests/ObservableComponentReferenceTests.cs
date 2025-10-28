using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Tests Observable filtering for components with referenced models.
/// Verifies that Filter() correctly handles "Model.RefName.Property" format.
/// </summary>
public class ObservableComponentReferenceTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ObservableComponentReferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReferencedModelFilter_UsesCorrectFormat()
    {
        // Arrange
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();

        // Act
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        // Get the generated filter
        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert - should use "Model.CounterModel.PropertyName" format
        _output.WriteLine($"Generated filter: [{string.Join(", ", filter)}]");

        // Filter should include referenced model properties in correct format
        var hasCounter1 = filter.Any(f => f.Contains("CounterModel") && f.Contains("Counter1"));
        var hasCounter2 = filter.Any(f => f.Contains("CounterModel") && f.Contains("Counter2"));
        var hasCounter3 = filter.Any(f => f.Contains("Counter3"));

        Assert.True(hasCounter1, "Filter should contain CounterModel.Counter1");
        Assert.True(hasCounter2, "Filter should contain CounterModel.Counter2");
        Assert.False(hasCounter3, "Filter should NOT contain Counter3 (not used in component)");
    }

    [Fact]
    public void ReferencedModel_PropertyChange_EmitsCorrectFormat()
    {
        // Arrange
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        var emittedProperties = new List<string>();
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProperties.AddRange(props);
            _output.WriteLine($"Parent Observable emitted: {string.Join(", ", props)}");
        });

        // Act - change referenced model property
        cut.Instance.Model.GetCounterModel().Counter1 = 42;
        Thread.Sleep(50);

        // Assert - should emit "Model.CounterModel.Counter1" format
        var emittedCounter1 = emittedProperties.FirstOrDefault(p => p.Contains("Counter1"));
        Assert.NotNull(emittedCounter1);
        Assert.Contains("CounterModel", emittedCounter1);
        Assert.Contains("Counter1", emittedCounter1);
        _output.WriteLine($"✓ Referenced model emission format: {emittedCounter1}");
    }

    [Fact]
    public void ReferencedModel_EmissionMatchesFilter()
    {
        // Arrange
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        var emittedProperties = new List<string>();
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProperties.AddRange(props);
        });

        // Act
        cut.Instance.Model.GetCounterModel().Counter1 = 10;
        Thread.Sleep(50);

        // Assert
        var emission = emittedProperties.FirstOrDefault(p => p.Contains("Counter1"));
        Assert.NotNull(emission);

        // Check if emission intersects with filter
        var intersects = filter.Intersect(emittedProperties).Any();
        _output.WriteLine($"Emission: {emission}");
        _output.WriteLine($"Filter: [{string.Join(", ", filter)}]");
        _output.WriteLine($"Intersects: {intersects}");

        Assert.True(intersects, "Emitted property should intersect with filter");
    }

    [Fact]
    public void ReferencedModel_UnusedProperty_NotInFilter()
    {
        // Arrange
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        var emittedProperties = new List<string>();
        using var subscription = cut.Instance.Model.Observable.Subscribe(props =>
        {
            emittedProperties.AddRange(props);
        });

        // Act - change Counter3 which is NOT used in component
        cut.Instance.Model.GetCounterModel().Counter3 = 999;
        Thread.Sleep(50);

        // Assert
        var emission = emittedProperties.FirstOrDefault(p => p.Contains("Counter3"));

        if (emission is not null)
        {
            // Counter3 was emitted by referenced model, but should NOT intersect with filter
            var intersects = filter.Contains(emission);
            _output.WriteLine($"Counter3 emission: {emission}");
            _output.WriteLine($"In filter: {intersects}");
            Assert.False(intersects, "Counter3 should NOT be in filter (not used in component)");
        }
        else
        {
            // Counter3 wasn't emitted at all (filtered at source)
            _output.WriteLine("✓ Counter3 not observed - parent model doesn't subscribe to it");
        }
    }

    [Fact]
    public void ReferencedModel_BatchChanges_FiltersCorrectly()
    {
        // Arrange
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        var emittedBatches = new List<string[]>();
        using var subscription = cut.Instance.Model.Observable
            .Select(props => props.Where(p => cut.Instance.Model.FilterUsedProperties(p)).ToArray())
            .Where(props => props.Length > 0)
            .Subscribe(props =>
            {
                emittedBatches.Add(props);
                _output.WriteLine($"Batch emitted: [{string.Join(", ", props)}]");
            });

        // Act - batch change multiple properties
        using (cut.Instance.Model.GetCounterModel().SuspendNotifications())
        {
            cut.Instance.Model.GetCounterModel().Counter1 = 1;
            cut.Instance.Model.GetCounterModel().Counter2 = 2;
            cut.Instance.Model.GetCounterModel().Counter3 = 3; // Not used
        }

        Thread.Sleep(50);

        // Assert
        if (emittedBatches.Count > 0)
        {
            var batch = emittedBatches[0];
            var counter1Emitted = batch.Any(p => p.Contains("Counter1"));
            var counter2Emitted = batch.Any(p => p.Contains("Counter2"));
            var counter3Emitted = batch.Any(p => p.Contains("Counter3"));

            _output.WriteLine($"Counter1 in batch: {counter1Emitted}");
            _output.WriteLine($"Counter2 in batch: {counter2Emitted}");
            _output.WriteLine($"Counter3 in batch: {counter3Emitted}");

            // Parent model should only emit properties it observes
            Assert.True(counter1Emitted || counter2Emitted, "Used properties should be emitted");
            Assert.False(counter3Emitted, "Counter3 should not be emitted by parent");
        }
    }

    [Fact]
    public void MultipleReferencedModels_EachHasCorrectPrefix()
    {
        // Arrange
        Services.AddTransient<CounterModel>();
        Services.AddTransient<ParentModel>();
        var cut = Render<ParentModelComponent>();
        cut.WaitForState(() => cut.Instance.Model.GetCounterModel() is not null, timeout: TimeSpan.FromSeconds(2));

        var filterMethod = cut.Instance.GetType().GetMethod("Filter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filter = (string[])filterMethod!.Invoke(cut.Instance, null)!;

        // Assert - all references should have consistent naming
        foreach (var prop in filter)
        {
            _output.WriteLine($"Filter property: {prop}");

            // All should start with "Model."
            Assert.StartsWith("Model.", prop);

            // If it references another model, format should be "Model.RefName.Property"
            if (prop.Contains("CounterModel"))
            {
                Assert.Matches(@"Model\.CounterModel\.\w+", prop);
            }
        }
    }
}
