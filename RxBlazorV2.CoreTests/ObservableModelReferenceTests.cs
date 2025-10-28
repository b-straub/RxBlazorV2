using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableModelReferenceTests
{
    private readonly ITestOutputHelper _output;

    public ObservableModelReferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParentModel_HasCounterModelReference()
    {
        // Arrange
        var counterModel = new CounterModel();

        // Act
        var parent = new ParentModel(counterModel);

        // Assert
        Assert.NotNull(parent.GetCounterModel());
        Assert.Same(counterModel, parent.GetCounterModel());
        _output.WriteLine($"CounterModel reference created: {parent.GetCounterModel().GetType().Name}");
    }

    [Fact]
    public void ParentModel_CounterModel_IsInitialized()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);

        // Act & Assert
        Assert.Equal(0, parent.GetCounterModel().Counter1);
        Assert.Equal(0, parent.GetCounterModel().Counter2);
        Assert.Equal(0, parent.GetCounterModel().Counter3);
        _output.WriteLine("CounterModel initialized with default values");
    }

    [Fact]
    public void ParentModel_ObservesCounter1Changes()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        using var subscription = parent.Observable.Subscribe(props =>
        {
            notificationCount++;
            receivedProperties.Add(props);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", props)}");
        });

        // Act
        counterModel.Counter1 = 5;

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Contains("Model.CounterModel.Counter1", receivedProperties[0]);
    }

    [Fact]
    public void ParentModel_ObservesCounter2Changes()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        using var subscription = parent.Observable.Subscribe(props =>
        {
            notificationCount++;
            receivedProperties.Add(props);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", props)}");
        });

        // Act
        counterModel.Counter2 = 10;

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Contains("Model.CounterModel.Counter2", receivedProperties[0]);
    }

    [Fact]
    public void ParentModel_DoesNotObserveCounter3Changes()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        using var subscription = parent.Observable
            .Select(props => props.Where(p => parent.FilterUsedProperties(p)).ToArray())
            .Where(props => props.Length > 0)
            .Subscribe(props =>
            {
                notificationCount++;
                receivedProperties.Add(props);
                _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", props)}");
            });

        // Act
        counterModel.Counter3 = 100;

        // Assert - Counter3 is not used in any parent property, so no notification should occur
        Assert.Equal(0, notificationCount);
        _output.WriteLine("Counter3 changes correctly ignored (not observed)");
    }

    [Fact]
    public void ParentModel_FiltersObservationsToUsedPropertiesOnly()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;
        var allProperties = new List<string>();

        using var subscription = parent.Observable
            .Select(props => props.Where(p => parent.FilterUsedProperties(p)).ToArray())
            .Where(props => props.Length > 0)
            .Subscribe(props =>
            {
                notificationCount++;
                allProperties.AddRange(props);
                _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", props)}");
            });

        // Act - change all counters
        counterModel.Counter1 = 1;
        counterModel.Counter2 = 2;
        counterModel.Counter3 = 3;

        // Assert - only Counter1 and Counter2 should trigger notifications
        Assert.Equal(2, notificationCount);
        Assert.Contains("Model.CounterModel.Counter1", allProperties);
        Assert.Contains("Model.CounterModel.Counter2", allProperties);
        Assert.DoesNotContain("Model.CounterModel.Counter3", allProperties);
        _output.WriteLine("Correctly filtered to Counter1 and Counter2 only");
    }

    [Fact]
    public void ParentModel_ComplexPropertyPattern_DetectsCorrectProperties()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        parent.AddMode = true;
        parent.Add10 = false;

        var notificationCount = 0;
        using var subscription = parent.Observable.Subscribe(_ => notificationCount++);

        // Act - change Counter1 and Counter2 which are used in IsValid
        counterModel.Counter1 = 5;
        counterModel.Counter2 = 10;

        // Assert
        Assert.Equal(2, notificationCount);
        Assert.True(parent.IsValid); // Counter1 < 10 and Counter2 < 20
        _output.WriteLine($"IsValid: {parent.IsValid} (Counter1={counterModel.Counter1}, Counter2={counterModel.Counter2})");
    }

    [Fact]
    public void ParentModel_MultiplePropertyAccess_TriggersNotifications()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var receivedProperties = new List<string[]>();

        using var subscription = parent.Observable.Subscribe(props =>
        {
            receivedProperties.Add(props);
            _output.WriteLine($"Notification: {string.Join(", ", props)}");
        });

        // Act
        counterModel.Counter1 = 3;
        counterModel.Counter2 = 7;

        // Assert
        Assert.Equal(2, receivedProperties.Count);
        Assert.Equal(10, parent.TotalCount); // Counter1 + Counter2
        _output.WriteLine($"TotalCount: {parent.TotalCount}");
    }

    [Fact]
    public void ParentModel_IsCounter1Low_TracksCounter1Only()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;

        using var subscription = parent.Observable.Subscribe(_ => notificationCount++);

        // Act
        counterModel.Counter1 = 3;

        // Assert
        Assert.True(parent.IsCounter1Low); // Counter1 < 5
        Assert.Equal(1, notificationCount);
        _output.WriteLine($"IsCounter1Low: {parent.IsCounter1Low}");
    }

    [Fact]
    public void ParentModel_IsCounter2High_TracksCounter2Only()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;

        using var subscription = parent.Observable.Subscribe(_ => notificationCount++);

        // Act
        counterModel.Counter2 = 18;

        // Assert
        Assert.True(parent.IsCounter2High); // Counter2 > 15
        Assert.Equal(1, notificationCount);
        _output.WriteLine($"IsCounter2High: {parent.IsCounter2High}");
    }

    [Fact]
    public void ParentModel_BatchChanges_OnReferencedModel()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        using var subscription = parent.Observable
            .Select(props => props.Where(p => parent.FilterUsedProperties(p)).ToArray())
            .Where(props => props.Length > 0)
            .Subscribe(props =>
            {
                notificationCount++;
                receivedProperties.Add(props);
                _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", props)}");
            });

        // Act - batch changes on CounterModel
        using (counterModel.SuspendNotifications())
        {
            counterModel.Counter1 = 1;
            counterModel.Counter2 = 2;
            counterModel.Counter3 = 3; // Not observed
        }

        // Assert - should get 1 batched notification with Counter1 and Counter2
        Assert.Equal(1, notificationCount);
        Assert.Equal(2, receivedProperties[0].Length);
        Assert.Contains("Model.CounterModel.Counter1", receivedProperties[0]);
        Assert.Contains("Model.CounterModel.Counter2", receivedProperties[0]);
        Assert.DoesNotContain("Model.CounterModel.Counter3", receivedProperties[0]);
    }

    [Fact]
    public void ParentModel_MultipleModels_IndependentNotifications()
    {
        // Arrange
        var counterModel1 = new CounterModel();
        var counterModel2 = new CounterModel();
        var parent1 = new ParentModel(counterModel1);
        var parent2 = new ParentModel(counterModel2);

        var parent1Notifications = 0;
        var parent2Notifications = 0;

        using var subscription1 = parent1.Observable.Subscribe(_ => parent1Notifications++);
        using var subscription2 = parent2.Observable.Subscribe(_ => parent2Notifications++);

        // Act - change only parent1's counter model
        counterModel1.Counter1 = 5;

        // Assert - only parent1 should be notified
        Assert.Equal(1, parent1Notifications);
        Assert.Equal(0, parent2Notifications);
        _output.WriteLine($"Parent1 notifications: {parent1Notifications}, Parent2 notifications: {parent2Notifications}");
    }

    [Fact]
    public void ParentModel_ObservableIntersect_FiltersCorrectly()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        var counter1Changes = 0;
        var counter2Changes = 0;
        var counter3Changes = 0;

        using var subscription = parent.Observable
            .Select(props => props.Where(p => parent.FilterUsedProperties(p)).ToArray())
            .Where(props => props.Length > 0)
            .Subscribe(props =>
            {
                if (props.Contains("Model.CounterModel.Counter1"))
                {
                    counter1Changes++;
                }
                if (props.Contains("Model.CounterModel.Counter2"))
                {
                    counter2Changes++;
                }
                if (props.Contains("Model.CounterModel.Counter3"))
                {
                    counter3Changes++;
                }
                _output.WriteLine($"Properties changed: {string.Join(", ", props)}");
            });

        // Act
        counterModel.Counter1 = 1;
        counterModel.Counter2 = 2;
        counterModel.Counter3 = 3;
        counterModel.Counter1 = 10;
        counterModel.Counter2 = 20;
        counterModel.Counter3 = 30;

        // Assert - Counter3 should never appear in notifications
        Assert.Equal(2, counter1Changes);
        Assert.Equal(2, counter2Changes);
        Assert.Equal(0, counter3Changes);
        _output.WriteLine($"Counter1: {counter1Changes}, Counter2: {counter2Changes}, Counter3: {counter3Changes}");
    }

    [Fact]
    public void ParentModel_PropertyPattern_WithNullCheck()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        parent.AddMode = true;
        parent.Add10 = false;

        // Act
        counterModel.Counter1 = 5;
        counterModel.Counter2 = 15;

        // Assert - IsValid uses pattern: CounterModel is { Counter2: < 20, Counter1: < 10 }
        Assert.True(parent.IsValid);
        _output.WriteLine($"IsValid with pattern matching: {parent.IsValid}");
    }

    [Fact]
    public void ParentModel_PropertyPattern_WhenConditionsFail()
    {
        // Arrange
        var counterModel = new CounterModel();
        var parent = new ParentModel(counterModel);
        parent.AddMode = true;
        parent.Add10 = false;

        // Act - set Counter1 >= 10 to fail the pattern
        counterModel.Counter1 = 10;
        counterModel.Counter2 = 5;

        // Assert - IsValid should be false (Counter1 >= 10)
        Assert.False(parent.IsValid);
        _output.WriteLine($"IsValid when pattern fails: {parent.IsValid}");
    }
}
