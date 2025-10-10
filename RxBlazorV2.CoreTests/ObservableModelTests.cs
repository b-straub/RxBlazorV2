using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableModelTests
{
    private readonly ITestOutputHelper _output;

    public ObservableModelTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PropertyChange_TriggersObservable()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        string? lastPropertyName = null;

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            lastPropertyName = properties.FirstOrDefault();
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        model.Counter = 5;

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Equal(nameof(model.Counter), lastPropertyName);
    }

    [Fact]
    public void MultiplePropertyChanges_TriggerMultipleNotifications()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        var notifications = new List<string[]>();

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            notifications.Add(properties);
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        model.Counter = 1;
        model.Name = "Test";
        model.Counter = 2;

        // Assert
        Assert.Equal(3, notificationCount);
        Assert.Equal(nameof(model.Counter), notifications[0][0]);
        Assert.Equal(nameof(model.Name), notifications[1][0]);
        Assert.Equal(nameof(model.Counter), notifications[2][0]);
    }

    [Fact]
    public void ManualStateChanged_WithPropertyName_TriggersObservable()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        string? lastPropertyName = null;

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            lastPropertyName = properties.FirstOrDefault();
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        model.TriggerStateChanged("CustomProperty");

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Equal("CustomProperty", lastPropertyName);
    }

    [Fact]
    public void ManualStateChanged_WithNullPropertyName_UsesModelID()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        string? lastPropertyName = null;

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            lastPropertyName = properties.FirstOrDefault();
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        model.TriggerStateChanged((string)null!);

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.Equal(model.ModelID, lastPropertyName);
    }

    [Fact]
    public void ManualStateChanged_WithMultipleProperties_TriggersObservableWithAllProperties()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;
        string[]? lastProperties = null;

        model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            lastProperties = properties;
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        model.TriggerStateChanged([nameof(model.Counter), nameof(model.Name)]);

        // Assert
        Assert.Equal(1, notificationCount);
        Assert.NotNull(lastProperties);
        Assert.Equal(2, lastProperties.Length);
        Assert.Contains(nameof(model.Counter), lastProperties);
        Assert.Contains(nameof(model.Name), lastProperties);
    }

    [Fact]
    public void Observable_SupportsMultipleSubscribers()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount1 = 0;
        var notificationCount2 = 0;

        model.Observable.Subscribe(_ => notificationCount1++);
        model.Observable.Subscribe(_ => notificationCount2++);

        // Act
        model.Counter = 5;

        // Assert
        Assert.Equal(1, notificationCount1);
        Assert.Equal(1, notificationCount2);
    }

    [Fact]
    public void Observable_Unsubscribe_StopsNotifications()
    {
        // Arrange
        var model = new TestObservableModel();
        var notificationCount = 0;

        var subscription = model.Observable.Subscribe(_ => notificationCount++);

        // Act
        model.Counter = 1;
        subscription.Dispose();
        model.Counter = 2;

        // Assert
        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public void ModelID_IsAccessible()
    {
        // Arrange & Act
        var model = new TestObservableModel();

        // Assert
        Assert.Equal("RxBlazorV2.CoreTests.TestFixtures.TestObservableModel", model.ModelID);
    }
}
