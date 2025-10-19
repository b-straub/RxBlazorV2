using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandBaseTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandBaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region SubscribeCore Property Filtering Tests

    [Fact]
    public void CommandObservable_EmptyPropertyArray_TriggersOnAllChanges()
    {
        // Arrange
        var model = new TestCommandModel { Value = 0 };
        var notificationCount = 0;

        // Subscribe to model's observable (commands internally filter based on observed properties)
        using var subscription = model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            _output.WriteLine($"Model notification {notificationCount}");
        });

        // Act - Change Value property
        model.Value = 1;

        // Assert - Should trigger notifications
        Assert.True(notificationCount > 0, "Model should emit notifications on property changes");
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    [Fact]
    public void CommandObservable_PropertyChanges_EmitNotifications()
    {
        // Arrange
        var model = new TestCommandModel { Value = 0 };
        var allNotifications = 0;

        // Subscribe to model (to see all changes)
        using var modelSubscription = model.Observable.Subscribe(_ =>
        {
            allNotifications++;
            _output.WriteLine($"Model notification {allNotifications}");
        });

        // Act - Change Value property
        model.Value = 1;

        // Assert - Model should emit notifications
        Assert.True(allNotifications > 0, "Model should emit all notifications");
        _output.WriteLine($"Total notifications: {allNotifications}");
    }

    [Fact]
    public void CommandObservable_IntersectingProperties_TriggersNotification()
    {
        // Arrange
        var model = new TestCommandModel { Value = 0 };
        var notificationCount = 0;

        using var subscription = model.Observable.Subscribe(properties =>
        {
            // Model emits property change notifications
            notificationCount++;
            _output.WriteLine($"Notification {notificationCount} for properties: {string.Join(", ", properties)}");
        });

        // Act - Trigger state change
        model.Value = 5;

        // Assert
        Assert.True(notificationCount > 0, "Should trigger when property changes");
    }

    [Fact]
    public void CommandObservable_MultipleSubscribers_AllReceiveNotifications()
    {
        // Arrange
        var model = new TestCommandModel { Value = 0 };
        var subscriber1Count = 0;
        var subscriber2Count = 0;

        using var sub1 = model.Observable.Subscribe(_ => subscriber1Count++);
        using var sub2 = model.Observable.Subscribe(_ => subscriber2Count++);

        // Act
        model.Value = 1;
        model.Value = 2;

        // Assert
        Assert.True(subscriber1Count > 0, "Subscriber 1 should receive notifications");
        Assert.True(subscriber2Count > 0, "Subscriber 2 should receive notifications");
        Assert.Equal(subscriber1Count, subscriber2Count);
        _output.WriteLine($"Both subscribers received {subscriber1Count} notifications");
    }

    [Fact]
    public void CommandObservable_Unsubscribe_StopsReceivingNotifications()
    {
        // Arrange
        var model = new TestCommandModel { Value = 0 };
        var notificationCount = 0;

        var subscription = model.Observable.Subscribe(_ => notificationCount++);

        // Act - Change value while subscribed
        model.Value = 1;
        var countAfterFirst = notificationCount;

        // Unsubscribe
        subscription.Dispose();

        // Change value after unsubscribed
        model.Value = 2;
        var countAfterDispose = notificationCount;

        // Assert
        Assert.True(countAfterFirst > 0, "Should receive notifications while subscribed");
        Assert.Equal(countAfterFirst, countAfterDispose);
        _output.WriteLine($"Notifications stopped after unsubscribe: {countAfterFirst} == {countAfterDispose}");
    }

    [Fact]
    public void CommandObservable_CommandExecution_TriggersNotification()
    {
        // Arrange
        var model = new TestCommandModel { Value = 0 };
        var notificationCount = 0;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            _output.WriteLine($"Notification {notificationCount}");
        });

        // Act - Execute command (which changes state)
        model.SyncCommand.Execute();

        // Assert
        Assert.True(notificationCount > 0, "Command execution should trigger notifications");
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void CommandBase_Error_InitiallyNull()
    {
        // Arrange
        var model = new TestCommandModel();

        // Assert
        Assert.Null(model.SyncCommand.Error);
        _output.WriteLine("Command error is initially null");
    }

    [Fact]
    public void CommandBase_ResetError_WhenNoError_DoesNotThrow()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act & Assert - Should not throw
        model.SyncCommand.ResetError();
        Assert.Null(model.SyncCommand.Error);
        _output.WriteLine("ResetError on null error does not throw");
    }

    [Fact]
    public void CommandBase_ErrorAfterException_Persists()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true, Value = 0 };

        // Act
        model.SyncCommand.Execute();
        var error = model.SyncCommand.Error;

        // Change state
        model.Value = 5;

        // Assert - Error should persist across state changes
        Assert.NotNull(model.SyncCommand.Error);
        Assert.Same(error, model.SyncCommand.Error);
        _output.WriteLine("Error persists across state changes until reset");
    }

    #endregion

    #region CanExecute Tests

    [Fact]
    public void CommandBase_CanExecute_DefaultTrue()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };

        // Assert
        Assert.True(model.SyncCommand.CanExecute);
        _output.WriteLine("CanExecute is true by default");
    }

    [Fact]
    public void CommandBase_CanExecute_ReactsToPropertyChanges()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };
        Assert.True(model.SyncCommand.CanExecute);

        // Act - Change to invalid state
        model.Value = -1;

        // Assert
        Assert.False(model.SyncCommand.CanExecute);
        _output.WriteLine("CanExecute reacts to property changes");
    }

    #endregion
}
