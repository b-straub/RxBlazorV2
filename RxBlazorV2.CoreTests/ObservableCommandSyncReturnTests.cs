using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandSyncReturnTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandSyncReturnTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region ObservableCommandRFactory<T> Tests (No Parameters)

    [Fact]
    public void SyncReturnCommand_Execute_ReturnsValue()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 5 };

        // Act
        var result = model.SyncReturnCommand.Execute();

        // Assert
        Assert.Equal(60, result); // (5 + 1) * 10
        Assert.Equal(6, model.Value);
        Assert.Equal(1, model.ExecuteCount);
        _output.WriteLine($"Sync return command returned: {result}");
    }

    [Fact]
    public void SyncReturnCommand_Execute_UpdatesValueAndReturnsResult()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result = model.SyncReturnCommand.Execute();

        // Assert
        Assert.Equal(1, model.Value);
        Assert.Equal(10, result);
        Assert.Contains(model.ExecutionLog, log => log == "SyncReturn: Value=1");
    }

    [Fact]
    public void SyncReturnCommand_MultipleExecutions_ReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = model.SyncReturnCommand.Execute();
        var result2 = model.SyncReturnCommand.Execute();
        var result3 = model.SyncReturnCommand.Execute();

        // Assert
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(30, result3);
        Assert.Equal(3, model.ExecuteCount);
        Assert.Equal(3, model.Value);
        _output.WriteLine($"Multiple executions: {result1}, {result2}, {result3}");
    }

    [Fact]
    public void SyncReturnCommand_Exception_StoresErrorAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };

        // Act
        var result = model.SyncReturnCommand.Execute();

        // Assert
        Assert.Null(result); // Should return default(int?) = null
        Assert.NotNull(model.SyncReturnCommand.Error);
        Assert.IsType<InvalidOperationException>(model.SyncReturnCommand.Error);
        Assert.Equal(0, model.ExecuteCount); // Should not complete
        _output.WriteLine($"Exception caught: {model.SyncReturnCommand.Error.Message}");
    }

    [Fact]
    public void SyncReturnCommand_ResetError_ClearsError()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };
        model.SyncReturnCommand.Execute();
        Assert.NotNull(model.SyncReturnCommand.Error);

        // Act
        model.SyncReturnCommand.ResetError();

        // Assert
        Assert.Null(model.SyncReturnCommand.Error);
        _output.WriteLine("Error was successfully reset");
    }

    [Fact]
    public void SyncReturnCommand_CanExecute_RespectsCondition()
    {
        // Arrange
        var model1 = new TestReturnCommandModel { Value = 5 };
        var model2 = new TestReturnCommandModel { Value = -1 };

        // Assert
        Assert.True(model1.SyncReturnCommand.CanExecute);
        Assert.False(model2.SyncReturnCommand.CanExecute);
        _output.WriteLine("CanExecute condition works correctly");
    }

    [Fact]
    public void SyncReturnCommand_StateChanges_TriggerNotifications()
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
        var result = model.SyncReturnCommand.Execute();

        // Assert
        Assert.True(notificationCount > 0, "Should trigger state change notification");
        Assert.Equal(10, result);
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    #endregion

    #region ObservableCommandRFactory<T1, T2> Tests (With Parameters)

    [Fact]
    public void SyncReturnCommandWithParam_Execute_ReturnsValue()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 10 };

        // Act
        var result = model.SyncReturnCommandWithParam.Execute(5);

        // Assert
        Assert.Equal("Result: 15", result);
        Assert.Equal(15, model.Value);
        Assert.Equal(5, model.LastParameter);
        Assert.Equal(1, model.ExecuteCount);
        _output.WriteLine($"Sync return with param returned: {result}");
    }

    [Fact]
    public void SyncReturnCommandWithParam_Execute_UpdatesValueAndReturnsResult()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result = model.SyncReturnCommandWithParam.Execute(7);

        // Assert
        Assert.Equal(7, model.Value);
        Assert.Equal("Result: 7", result);
        Assert.Contains(model.ExecutionLog, log => log == "SyncReturnWithParam: param=7, Value=7");
    }

    [Fact]
    public void SyncReturnCommandWithParam_MultipleExecutions_ReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = model.SyncReturnCommandWithParam.Execute(1);
        var result2 = model.SyncReturnCommandWithParam.Execute(2);
        var result3 = model.SyncReturnCommandWithParam.Execute(3);

        // Assert
        Assert.Equal("Result: 1", result1);
        Assert.Equal("Result: 3", result2);
        Assert.Equal("Result: 6", result3);
        Assert.Equal(3, model.ExecuteCount);
        Assert.Equal(6, model.Value); // 0 + 1 + 2 + 3
        _output.WriteLine($"Multiple executions: {result1}, {result2}, {result3}");
    }

    [Fact]
    public void SyncReturnCommandWithParam_DifferentParameters_HandledCorrectly()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = model.SyncReturnCommandWithParam.Execute(10);
        var result2 = model.SyncReturnCommandWithParam.Execute(20);
        var result3 = model.SyncReturnCommandWithParam.Execute(30);

        // Assert
        Assert.Equal(30, model.LastParameter);
        Assert.Equal(60, model.Value); // 0 + 10 + 20 + 30
        Assert.Equal("Result: 10", result1);
        Assert.Equal("Result: 30", result2);
        Assert.Equal("Result: 60", result3);
        _output.WriteLine($"Different parameters handled, final value: {model.Value}");
    }

    [Fact]
    public void SyncReturnCommandWithParam_Exception_StoresErrorAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };

        // Act
        var result = model.SyncReturnCommandWithParam.Execute(5);

        // Assert
        Assert.Null(result); // Should return default(string?) = null
        Assert.NotNull(model.SyncReturnCommandWithParam.Error);
        Assert.IsType<InvalidOperationException>(model.SyncReturnCommandWithParam.Error);
        Assert.Equal(0, model.ExecuteCount); // Should not complete
        _output.WriteLine($"Exception caught: {model.SyncReturnCommandWithParam.Error.Message}");
    }

    [Fact]
    public void SyncReturnCommandWithParam_ResetError_ClearsError()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };
        model.SyncReturnCommandWithParam.Execute(5);
        Assert.NotNull(model.SyncReturnCommandWithParam.Error);

        // Act
        model.SyncReturnCommandWithParam.ResetError();

        // Assert
        Assert.Null(model.SyncReturnCommandWithParam.Error);
        _output.WriteLine("Error was successfully reset");
    }

    [Fact]
    public void SyncReturnCommandWithParam_CanExecute_RespectsCondition()
    {
        // Arrange
        var model1 = new TestReturnCommandModel { Value = 5 };
        var model2 = new TestReturnCommandModel { Value = -1 };

        // Assert
        Assert.True(model1.SyncReturnCommandWithParam.CanExecute);
        Assert.False(model2.SyncReturnCommandWithParam.CanExecute);
        _output.WriteLine("CanExecute condition works correctly");
    }

    [Fact]
    public void SyncReturnCommandWithParam_StateChanges_TriggerNotifications()
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
        var result = model.SyncReturnCommandWithParam.Execute(10);

        // Assert
        Assert.True(notificationCount > 0, "Should trigger state change notification");
        Assert.Equal("Result: 10", result);
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    [Fact]
    public void SyncReturnCommandWithParam_ZeroParameter_HandledCorrectly()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 5 };

        // Act
        var result = model.SyncReturnCommandWithParam.Execute(0);

        // Assert
        Assert.Equal(0, model.LastParameter);
        Assert.Equal(5, model.Value); // No change
        Assert.Equal("Result: 5", result);
        _output.WriteLine($"Zero parameter handled correctly");
    }

    [Fact]
    public void SyncReturnCommandWithParam_NegativeParameter_HandledCorrectly()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 10 };

        // Act
        var result = model.SyncReturnCommandWithParam.Execute(-3);

        // Assert
        Assert.Equal(-3, model.LastParameter);
        Assert.Equal(7, model.Value); // 10 + (-3)
        Assert.Equal("Result: 7", result);
        _output.WriteLine($"Negative parameter handled correctly");
    }

    #endregion
}
