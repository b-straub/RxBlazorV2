using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandReturnCancellationTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandReturnCancellationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region ObservableCommandRAsyncCancelableFactory<T> Tests

    [Fact]
    public async Task CancelableReturnCommand_Cancel_CancelsExecutionAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var taskStarted = false;
        var taskCancelled = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableReturnCommand.Executing)
            {
                taskStarted = true;
                model.CancelableReturnCommand.Cancel();
            }
            else if (taskStarted)
            {
                taskCancelled = true;
            }
        });

        // Act
        var result = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.True(taskStarted);
        Assert.True(taskCancelled);
        Assert.Equal(0, model.ExecuteCount); // Should not complete
        Assert.Equal(0, model.Value); // Should not update
        Assert.Null(result); // Should return default (null for int?)
        _output.WriteLine("Cancelable return command was successfully cancelled");
    }

    [Fact]
    public async Task CancelableReturnCommand_WithoutCancel_CompletesAndReturnsValue()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 5 };

        // Act
        var result = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(6, model.Value);
        Assert.Equal(60, result); // (5 + 1) * 10
        Assert.False(model.CancelableReturnCommand.Executing);
        _output.WriteLine($"Command completed successfully with result: {result}");
    }

    [Fact]
    public async Task CancelableReturnCommand_WithSuspension_FirstCommandBypassesSuspension()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var notificationCount = 0;
        var notificationsDuringExecution = 0;

        model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            if (model.CancelableReturnCommand.Executing)
            {
                notificationsDuringExecution++;
            }
            _output.WriteLine($"Notification {notificationCount}, Executing: {model.CancelableReturnCommand.Executing}");
        });

        // Act
        int? result;
        using (model.SuspendNotifications())
        {
            result = await model.CancelableReturnCommand.ExecuteAsync();
        }

        // Assert - First command should bypass suspension for immediate UI feedback
        Assert.True(notificationsDuringExecution > 0, "First command should trigger notification immediately");
        Assert.Equal(10, result);
        _output.WriteLine($"Notifications during execution: {notificationsDuringExecution}");
    }

    [Fact]
    public async Task CancelableReturnCommand_Cancel_AbortsSuspension()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var suspensionCompleted = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableReturnCommand.Executing)
            {
                model.CancelableReturnCommand.Cancel();
            }
        });

        // Act
        int? result;
        using (model.SuspendNotifications())
        {
            result = await model.CancelableReturnCommand.ExecuteAsync();
            suspensionCompleted = true;
        }

        // Assert
        Assert.True(suspensionCompleted);
        Assert.Equal(0, model.ExecuteCount);
        Assert.Null(result); // Should return default due to cancellation
        _output.WriteLine("Suspension aborted due to cancellation");
    }

    [Fact]
    public async Task CancelableReturnCommand_Exception_StoresErrorAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };

        // Act
        var result = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.NotNull(model.CancelableReturnCommand.Error);
        Assert.IsType<InvalidOperationException>(model.CancelableReturnCommand.Error);
        Assert.False(model.CancelableReturnCommand.Executing);
        Assert.Null(result); // Should return default due to exception
        _output.WriteLine($"Exception caught: {model.CancelableReturnCommand.Error.Message}");
    }

    [Fact]
    public async Task CancelableReturnCommand_ResetError_ClearsError()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };
        await model.CancelableReturnCommand.ExecuteAsync();
        Assert.NotNull(model.CancelableReturnCommand.Error);

        // Act
        model.CancelableReturnCommand.ResetError();

        // Assert
        Assert.Null(model.CancelableReturnCommand.Error);
        _output.WriteLine("Error was successfully reset");
    }

    [Fact]
    public async Task CancelableReturnCommand_MultipleExecutions_ResetsTokenAndReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = await model.CancelableReturnCommand.ExecuteAsync();
        var result2 = await model.CancelableReturnCommand.ExecuteAsync();
        var result3 = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.Equal(3, model.ExecuteCount);
        Assert.Equal(3, model.Value);
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(30, result3);
        _output.WriteLine("Multiple executions work correctly with token reset");
    }

    [Fact]
    public async Task CancelableReturnCommand_StateChanges_TriggerNotifications()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var stateChanges = new List<string>();

        using var subscription = model.Observable.Subscribe(props =>
        {
            stateChanges.Add(string.Join(", ", props));
            _output.WriteLine($"State changed: {string.Join(", ", props)}");
        });

        // Act
        var result = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.True(stateChanges.Count > 0);
        Assert.Equal(10, result);
        _output.WriteLine($"Total state changes: {stateChanges.Count}");
    }

    [Fact]
    public void CancelableReturnCommand_CanExecute_RespectsCondition()
    {
        // Arrange
        var model1 = new TestReturnCommandModel { Value = 5 };
        var model2 = new TestReturnCommandModel { Value = -1 };

        // Assert
        Assert.True(model1.CancelableReturnCommand.CanExecute);
        Assert.False(model2.CancelableReturnCommand.CanExecute);
        _output.WriteLine("CanExecute condition works correctly");
    }

    [Fact]
    public async Task CancelableReturnCommand_AfterCancellation_ExecutesNormallyAgain()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableReturnCommand.Executing)
            {
                model.CancelableReturnCommand.Cancel();
            }
        });

        // Act - First execution gets cancelled
        var result1 = await model.CancelableReturnCommand.ExecuteAsync();

        subscription.Dispose(); // Stop cancelling

        // Execute again, this time without cancellation
        var result2 = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.Null(result1); // First execution was cancelled
        Assert.Equal(10, result2); // Second execution completed normally
        Assert.Equal(1, model.ExecuteCount); // Only second execution completed
        _output.WriteLine($"After cancellation, command executed normally with result: {result2}");
    }

    #endregion

    #region ObservableCommandRAsyncCancelableFactory<T1, T2> Tests

    [Fact]
    public async Task CancelableReturnCommandWithParam_Cancel_CancelsExecutionAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var taskStarted = false;
        var taskCancelled = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableReturnCommandWithParam.Executing)
            {
                taskStarted = true;
                model.CancelableReturnCommandWithParam.Cancel();
            }
            else if (taskStarted)
            {
                taskCancelled = true;
            }
        });

        // Act
        var result = await model.CancelableReturnCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.True(taskStarted);
        Assert.True(taskCancelled);
        Assert.Equal(0, model.ExecuteCount); // Should not complete
        Assert.Equal(0, model.Value); // Should not update
        Assert.Null(result); // Should return default (null for string?)
        _output.WriteLine("Cancelable return command with param was successfully cancelled");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_WithoutCancel_CompletesAndReturnsValue()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 10 };

        // Act
        var result = await model.CancelableReturnCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(5, model.LastParameter);
        Assert.Equal(15, model.Value);
        Assert.Equal("Result: 15", result);
        Assert.False(model.CancelableReturnCommandWithParam.Executing);
        _output.WriteLine($"Command with param completed successfully with result: {result}");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_WithSuspension_FirstCommandBypassesSuspension()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var notificationCount = 0;
        var notificationsDuringExecution = 0;

        model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            if (model.CancelableReturnCommandWithParam.Executing)
            {
                notificationsDuringExecution++;
            }
            _output.WriteLine($"Notification {notificationCount}, Executing: {model.CancelableReturnCommandWithParam.Executing}");
        });

        // Act
        string? result;
        using (model.SuspendNotifications())
        {
            result = await model.CancelableReturnCommandWithParam.ExecuteAsync(10);
        }

        // Assert - First command should bypass suspension for immediate UI feedback
        Assert.True(notificationsDuringExecution > 0, "First command should trigger notification immediately");
        Assert.Equal("Result: 10", result);
        _output.WriteLine($"Notifications during execution: {notificationsDuringExecution}");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_Cancel_AbortsSuspension()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var suspensionCompleted = false;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.CancelableReturnCommandWithParam.Executing)
            {
                model.CancelableReturnCommandWithParam.Cancel();
            }
        });

        // Act
        string? result;
        using (model.SuspendNotifications())
        {
            result = await model.CancelableReturnCommandWithParam.ExecuteAsync(5);
            suspensionCompleted = true;
        }

        // Assert
        Assert.True(suspensionCompleted);
        Assert.Equal(0, model.ExecuteCount);
        Assert.Null(result); // Should return default due to cancellation
        _output.WriteLine("Suspension aborted due to cancellation");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_Exception_StoresErrorAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };

        // Act
        var result = await model.CancelableReturnCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.NotNull(model.CancelableReturnCommandWithParam.Error);
        Assert.IsType<InvalidOperationException>(model.CancelableReturnCommandWithParam.Error);
        Assert.False(model.CancelableReturnCommandWithParam.Executing);
        Assert.Null(result); // Should return default due to exception
        _output.WriteLine($"Exception caught: {model.CancelableReturnCommandWithParam.Error.Message}");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_MultipleExecutions_ResetsTokenAndReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = await model.CancelableReturnCommandWithParam.ExecuteAsync(1);
        var result2 = await model.CancelableReturnCommandWithParam.ExecuteAsync(2);
        var result3 = await model.CancelableReturnCommandWithParam.ExecuteAsync(3);

        // Assert
        Assert.Equal(3, model.ExecuteCount);
        Assert.Equal(6, model.Value); // 0 + 1 + 2 + 3
        Assert.Equal("Result: 1", result1);
        Assert.Equal("Result: 3", result2);
        Assert.Equal("Result: 6", result3);
        _output.WriteLine("Multiple executions with params work correctly with token reset");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_StateChanges_TriggerNotifications()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var stateChanges = new List<string>();

        using var subscription = model.Observable.Subscribe(props =>
        {
            stateChanges.Add(string.Join(", ", props));
            _output.WriteLine($"State changed: {string.Join(", ", props)}");
        });

        // Act
        var result = await model.CancelableReturnCommandWithParam.ExecuteAsync(10);

        // Assert
        Assert.True(stateChanges.Count > 0);
        Assert.Equal("Result: 10", result);
        _output.WriteLine($"Total state changes: {stateChanges.Count}");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_DifferentParameters_ReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = await model.CancelableReturnCommandWithParam.ExecuteAsync(10);
        var result2 = await model.CancelableReturnCommandWithParam.ExecuteAsync(20);
        var result3 = await model.CancelableReturnCommandWithParam.ExecuteAsync(30);

        // Assert
        Assert.Equal(30, model.LastParameter); // Last parameter should be 30
        Assert.Equal(60, model.Value); // 0 + 10 + 20 + 30
        Assert.Equal("Result: 10", result1);
        Assert.Equal("Result: 30", result2);
        Assert.Equal("Result: 60", result3);
        _output.WriteLine($"Different parameters handled correctly, final value: {model.Value}");
    }

    #endregion

    #region Edge Cases and State Management

    [Fact]
    public async Task CancelableReturnCommand_Executing_IsTrueDuringExecution()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var executingStates = new List<bool>();

        using var subscription = model.Observable.Subscribe(_ =>
        {
            executingStates.Add(model.CancelableReturnCommand.Executing);
            _output.WriteLine($"Executing: {model.CancelableReturnCommand.Executing}");
        });

        // Act
        await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.True(executingStates.Count >= 2, $"Expected at least 2 states, got {executingStates.Count}");
        Assert.True(executingStates.Any(s => s), "Expected Executing to be true at some point");
        Assert.False(executingStates.Last(), "Expected Executing to be false at end");
    }

    [Fact]
    public void CancelableReturnCommand_Cancel_ThrowsIfNotExecuting()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => model.CancelableReturnCommand.Cancel());
        _output.WriteLine($"Expected exception when cancelling non-executing command: {exception.GetType().Name}");
    }

    [Fact]
    public async Task CancelableReturnCommand_PropertyChanges_NotifyDuringExecution()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var notificationCount = 0;

        using var subscription = model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            _output.WriteLine($"Notification {notificationCount}, Value: {model.Value}");
        });

        // Act
        await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.True(notificationCount > 0, "Should receive notifications during execution");
        Assert.Equal(1, model.Value);
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    #endregion
}
