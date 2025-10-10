using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandCancellationTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandCancellationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CancelableCommand_Cancel_CancelsExecution()
    {
        // Arrange
        var model = new TestCommandModel();
        var taskStarted = false;
        var taskCancelled = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableCommand.Executing)
            {
                taskStarted = true;
                model.CancelableCommand.Cancel();
            }
            else if (taskStarted)
            {
                taskCancelled = true;
            }
        });

        // Act
        await model.CancelableCommand.ExecuteAsync();

        // Assert
        Assert.True(taskStarted);
        Assert.True(taskCancelled);
        Assert.Equal(0, model.ExecuteCount); // Should not complete
        Assert.Equal(0, model.Value); // Should not update
        _output.WriteLine("Cancelable command was successfully cancelled");
    }

    [Fact]
    public async Task CancelableCommand_WithoutCancel_CompletesNormally()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        await model.CancelableCommand.ExecuteAsync();

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(1, model.Value);
        Assert.False(model.CancelableCommand.Executing);
    }

    [Fact]
    public void CancelableCommand_Cancel_ThrowsIfNotExecuting()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => model.CancelableCommand.Cancel());
        _output.WriteLine($"Expected exception when cancelling non-executing command: {exception.GetType().Name}");
    }

    [Fact]
    public async Task CancelableCommand_Executing_IsTrueDuringExecution()
    {
        // Arrange
        var model = new TestCommandModel();
        var executingStates = new List<bool>();

        using var subscription = model.Observable.Subscribe(_ =>
        {
            executingStates.Add(model.CancelableCommand.Executing);
            _output.WriteLine($"Executing: {model.CancelableCommand.Executing}");
        });

        // Act
        await model.CancelableCommand.ExecuteAsync();

        // Assert
        Assert.True(executingStates.Count >= 2, $"Expected at least 2 states, got {executingStates.Count}");
        Assert.True(executingStates.Any(s => s), "Expected Executing to be true at some point");
        Assert.False(executingStates.Last(), "Expected Executing to be false at end");
    }

    [Fact]
    public async Task CancelableCommandWithParam_Cancel_CancelsExecution()
    {
        // Arrange
        var model = new TestCommandModel();
        var taskStarted = false;
        var taskCancelled = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableAsyncCommandWithParam.Executing)
            {
                taskStarted = true;
                model.CancelableAsyncCommandWithParam.Cancel();
            }
            else if (taskStarted)
            {
                taskCancelled = true;
            }
        });

        // Act
        await model.CancelableAsyncCommandWithParam.ExecuteAsync(10);

        // Assert
        Assert.True(taskStarted);
        Assert.True(taskCancelled);
        Assert.Equal(0, model.ExecuteCount);
        Assert.Equal(0, model.Value);
    }

    [Fact]
    public async Task CancelableCommandWithParam_WithoutCancel_CompletesNormally()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        await model.CancelableAsyncCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(5, model.LastParameter);
        Assert.Equal(5, model.Value);
    }

    [Fact]
    public async Task CancelableCommand_WithSuspension_FirstCommandBypassesSuspension()
    {
        // Arrange
        var model = new TestCommandModel();
        var notificationCount = 0;
        var notificationsDuringExecution = 0;

        model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            if (model.CancelableCommand.Executing)
            {
                notificationsDuringExecution++;
            }
            _output.WriteLine($"Notification {notificationCount}, Executing: {model.CancelableCommand.Executing}");
        });

        // Act
        using (model.SuspendNotifications())
        {
            await model.CancelableCommand.ExecuteAsync();
        }

        // Assert - First command should bypass suspension for immediate UI feedback
        Assert.True(notificationsDuringExecution > 0, "First command should trigger notification immediately");
        _output.WriteLine($"Notifications during execution: {notificationsDuringExecution}");
    }

    [Fact]
    public async Task CancelableCommand_Cancel_AbortsSuspension()
    {
        // Arrange
        var model = new TestCommandModel();
        var suspensionCompleted = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableCommand.Executing)
            {
                model.CancelableCommand.Cancel();
            }
        });

        // Act
        using (model.SuspendNotifications())
        {
            await model.CancelableCommand.ExecuteAsync();
            suspensionCompleted = true;
        }

        // Assert
        Assert.True(suspensionCompleted);
        Assert.Equal(0, model.ExecuteCount);
        _output.WriteLine("Suspension aborted due to cancellation");
    }

    [Fact]
    public async Task CancelableCommand_ExecuteAsync_CatchesException()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };

        // Act
        await model.CancelableCommand.ExecuteAsync();

        // Assert
        Assert.NotNull(model.CancelableCommand.Error);
        Assert.IsType<InvalidOperationException>(model.CancelableCommand.Error);
        Assert.False(model.CancelableCommand.Executing);
    }

    [Fact]
    public void CancelableCommand_CanExecute_WhenConditionIsTrue()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };

        // Assert
        Assert.True(model.CancelableCommand.CanExecute);
    }

    [Fact]
    public void CancelableCommand_CanExecute_WhenConditionIsFalse()
    {
        // Arrange
        var model = new TestCommandModel { Value = -1 };

        // Assert
        Assert.False(model.CancelableCommand.CanExecute);
    }

    [Fact]
    public async Task CancelableCommand_MultipleExecutions_ResetsToken()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        await model.CancelableCommand.ExecuteAsync();
        await model.CancelableCommand.ExecuteAsync();
        await model.CancelableCommand.ExecuteAsync();

        // Assert
        Assert.Equal(3, model.ExecuteCount);
        Assert.Equal(3, model.Value);
        _output.WriteLine("Multiple executions work correctly with token reset");
    }
}
