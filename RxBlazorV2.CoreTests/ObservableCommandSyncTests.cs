using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandSyncTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandSyncTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SyncCommand_Execute_UpdatesValue()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        model.SyncCommand.Execute();

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(1, model.Value);
    }

    [Fact]
    public void SyncCommand_CanExecute_WhenConditionIsTrue()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };

        // Assert
        Assert.True(model.SyncCommand.CanExecute);
    }

    [Fact]
    public void SyncCommand_CanExecute_WhenConditionIsFalse()
    {
        // Arrange
        var model = new TestCommandModel { Value = -1 };

        // Assert
        Assert.False(model.SyncCommand.CanExecute);
    }

    [Fact]
    public void SyncCommand_Execute_NotifiesObservers()
    {
        // Arrange
        var model = new TestCommandModel();
        var notificationCount = 0;

        using var subscription = model.Observable.Subscribe(properties =>
        {
            notificationCount++;
            _output.WriteLine($"Notification {notificationCount}: {string.Join(", ", properties)}");
        });

        // Act
        model.SyncCommand.Execute();

        // Assert
        Assert.True(notificationCount >= 1, $"Expected at least 1 notification, got {notificationCount}");
    }

    [Fact]
    public void SyncCommand_Execute_CatchesException()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };

        // Act
        model.SyncCommand.Execute();

        // Assert
        Assert.NotNull(model.SyncCommand.Error);
        Assert.IsType<InvalidOperationException>(model.SyncCommand.Error);
        Assert.Equal("Test exception", model.SyncCommand.Error.Message);
        _output.WriteLine($"Exception caught: {model.SyncCommand.Error.Message}");
    }

    [Fact]
    public void SyncCommand_ResetError_ClearsError()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };
        model.SyncCommand.Execute();
        Assert.NotNull(model.SyncCommand.Error);

        // Act
        model.SyncCommand.ResetError();

        // Assert
        Assert.Null(model.SyncCommand.Error);
    }

    [Fact]
    public void SyncCommand_ExecuteAgain_ResetsError()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };
        model.SyncCommand.Execute();
        Assert.NotNull(model.SyncCommand.Error);

        // Act
        model.ThrowException = false;
        model.SyncCommand.Execute();

        // Assert
        Assert.Null(model.SyncCommand.Error);
        Assert.Equal(1, model.Value); // First execution threw, second succeeded
    }

    [Fact]
    public void SyncCommandWithParam_Execute_UpdatesValueWithParameter()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        model.SyncCommandWithParam.Execute(5);

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(5, model.LastParameter);
        Assert.Equal(5, model.Value);
    }

    [Fact]
    public void SyncCommandWithParam_Execute_MultipleParameters()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        model.SyncCommandWithParam.Execute(3);
        model.SyncCommandWithParam.Execute(7);

        // Assert
        Assert.Equal(2, model.ExecuteCount);
        Assert.Equal(7, model.LastParameter);
        Assert.Equal(10, model.Value);
    }

    [Fact]
    public void SyncCommandWithParam_CanExecute_WhenConditionIsTrue()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };

        // Assert
        Assert.True(model.SyncCommandWithParam.CanExecute);
    }

    [Fact]
    public void SyncCommandWithParam_CanExecute_WhenConditionIsFalse()
    {
        // Arrange
        var model = new TestCommandModel { Value = -1 };

        // Assert
        Assert.False(model.SyncCommandWithParam.CanExecute);
    }

    [Fact]
    public void SyncCommandWithParam_Execute_CatchesException()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };

        // Act
        model.SyncCommandWithParam.Execute(10);

        // Assert
        Assert.NotNull(model.SyncCommandWithParam.Error);
        Assert.IsType<InvalidOperationException>(model.SyncCommandWithParam.Error);
    }

    [Fact]
    public void SyncCommand_PropertyChange_TriggersModelObservable()
    {
        // Arrange
        var model = new TestCommandModel();
        var notificationCount = 0;

        // Subscribe to the model's observable
        using var subscription = model.Observable.Subscribe(_ => notificationCount++);

        // Act
        model.Value = 10; // Should trigger notification

        // Assert
        Assert.Equal(1, notificationCount);
        _output.WriteLine("Model observable triggered on property change");
    }
}
