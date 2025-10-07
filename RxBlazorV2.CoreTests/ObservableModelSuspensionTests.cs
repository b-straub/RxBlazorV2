using RxBlazorV2.CoreTests.TestFixtures;
using Xunit.Abstractions;

namespace RxBlazorV2.CoreTests;

public class ObservableModelSuspensionTests
{
    private readonly ITestOutputHelper _output;

    public ObservableModelSuspensionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SuspendNotifications_BatchesPropertyChanges()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            receivedProperties.Add(properties);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        using (model.SuspendNotifications())
        {
            model.Counter = 1;
            model.Name = "Test";
            model.Counter = 2;
        }

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Equal(2, receivedProperties[0].Length);
        Assert.Contains(nameof(model.Counter), receivedProperties[0]);
        Assert.Contains(nameof(model.Name), receivedProperties[0]);
    }

    [Fact]
    public void SuspendNotifications_WithoutChanges_DoesNotNotify()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;

        model.Observable.Subscribe(_ => notificationCount++);

        // Act
        using (model.SuspendNotifications())
        {
            // No property changes
        }

        // Assert
        Assert.Equal(0, notificationCount);
        _output.WriteLine("No notifications when no changes during suspension");
    }

    [Fact]
    public void SuspendNotifications_DuplicateProperties_OnlyNotifiesOnce()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            receivedProperties.Add(properties);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        using (model.SuspendNotifications())
        {
            model.Counter = 1;
            model.Counter = 2;
            model.Counter = 3;
        }

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Single(receivedProperties[0]);
        Assert.Equal(nameof(model.Counter), receivedProperties[0][0]);
    }

    [Fact]
    public void SuspendNotifications_WithBatchId_OnlyAffectsBatchedProperties()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            receivedProperties.Add(properties);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        using (model.SuspendNotifications("batch1"))
        {
            model.Counter = 1; // Not batched - should notify immediately
            model.BatchProperty1 = 5; // Batched with "batch1" - should be suspended
            model.Name = "Test"; // Not batched - should notify immediately
        }

        // Assert - 3 notifications: Counter, Name (immediate), then BatchProperty1 (at end)
        Assert.Equal(3, notificationCount);
        Assert.Equal(nameof(model.Counter), receivedProperties[0][0]);
        Assert.Equal(nameof(model.Name), receivedProperties[1][0]);
        Assert.Equal(nameof(model.BatchProperty1), receivedProperties[2][0]);
    }

    [Fact]
    public void SuspendNotifications_WithMultipleBatchIds_SuspendsAllMatchingBatches()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            receivedProperties.Add(properties);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        using (model.SuspendNotifications("batch1", "batch2"))
        {
            model.Counter = 1; // Not batched - should notify immediately
            model.BatchProperty1 = 5; // Batched with "batch1" - should be suspended
            model.BatchProperty2 = 10; // Batched with "batch1" and "batch2" - should be suspended
        }

        // Assert - 2 notifications: Counter (immediate), then batched properties at end
        Assert.Equal(2, notificationCount);
        Assert.Equal(nameof(model.Counter), receivedProperties[0][0]);
        Assert.Equal(2, receivedProperties[1].Length);
        Assert.Contains(nameof(model.BatchProperty1), receivedProperties[1]);
        Assert.Contains(nameof(model.BatchProperty2), receivedProperties[1]);
    }

    [Fact]
    public void SuspendNotifications_Nested_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act & Assert
        using (model.SuspendNotifications())
        {
            var exception = Assert.Throws<InvalidOperationException>(() => model.SuspendNotifications());
            Assert.Contains("already active", exception.Message);
            _output.WriteLine($"Expected exception: {exception.Message}");
        }
    }

    [Fact]
    public void SuspendNotifications_AfterDispose_AllowsNewSuspension()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;

        model.Observable.Subscribe(_ => notificationCount++);

        // Act
        using (model.SuspendNotifications())
        {
            model.Counter = 1;
        }

        using (model.SuspendNotifications())
        {
            model.Counter = 2;
        }

        // Assert
        Assert.Equal(2, notificationCount);
        _output.WriteLine("Sequential suspensions work correctly");
    }

    [Fact]
    public void SuspendNotifications_EarlyDispose_FiresNotifications()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        var receivedProperties = new List<string[]>();

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            receivedProperties.Add(properties);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        var suspender = model.SuspendNotifications();
        model.Counter = 1;
        model.Name = "Test";
        suspender.Dispose(); // Early dispose

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Equal(2, receivedProperties[0].Length);
    }

    [Fact]
    public void SuspendNotifications_DoubleDispose_IsSafe()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;

        model.Observable.Subscribe(_ => notificationCount++);

        // Act
        var suspender = model.SuspendNotifications();
        model.Counter = 1;
        suspender.Dispose();
        suspender.Dispose(); // Double dispose

        // Assert
        Assert.Equal(1, notificationCount);
        _output.WriteLine("Double dispose is safe");
    }
}
