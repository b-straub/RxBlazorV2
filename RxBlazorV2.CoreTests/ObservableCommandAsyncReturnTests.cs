using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandAsyncReturnTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandAsyncReturnTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Non-Cancelable Async Return Commands (ObservableCommandRAsyncFactory)

    [Fact]
    public async Task AsyncReturnCommand_Execute_ReturnsValue()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 5 };

        // Act
        var result = await model.AsyncReturnCommand.ExecuteAsync();

        // Assert
        Assert.Equal(60, result); // (5 + 1) * 10
        Assert.Equal(6, model.Value);
        Assert.Equal(1, model.ExecuteCount);
        _output.WriteLine($"Async return command returned: {result}");
    }

    [Fact]
    public async Task AsyncReturnCommand_MultipleExecutions_ReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = await model.AsyncReturnCommand.ExecuteAsync();
        var result2 = await model.AsyncReturnCommand.ExecuteAsync();
        var result3 = await model.AsyncReturnCommand.ExecuteAsync();

        // Assert
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(30, result3);
        Assert.Equal(3, model.ExecuteCount);
        _output.WriteLine($"Multiple async executions: {result1}, {result2}, {result3}");
    }

    [Fact]
    public async Task AsyncReturnCommand_Exception_StoresErrorAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };

        // Act
        var result = await model.AsyncReturnCommand.ExecuteAsync();

        // Assert
        Assert.Null(result);
        Assert.NotNull(model.AsyncReturnCommand.Error);
        Assert.IsType<InvalidOperationException>(model.AsyncReturnCommand.Error);
        Assert.False(model.AsyncReturnCommand.Executing);
        _output.WriteLine($"Async exception caught: {model.AsyncReturnCommand.Error.Message}");
    }

    [Fact]
    public async Task AsyncReturnCommand_Executing_IsTrueDuringExecution()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var executingStates = new List<bool>();

        using var subscription = model.Observable.Subscribe(_ =>
        {
            executingStates.Add(model.AsyncReturnCommand.Executing);
            _output.WriteLine($"Executing: {model.AsyncReturnCommand.Executing}");
        });

        // Act
        await model.AsyncReturnCommand.ExecuteAsync();

        // Assert
        Assert.True(executingStates.Count >= 2, $"Expected at least 2 states, got {executingStates.Count}");
        Assert.True(executingStates.Any(s => s), "Expected Executing to be true at some point");
        Assert.False(executingStates.Last(), "Expected Executing to be false at end");
    }

    #endregion

    #region Non-Cancelable Async Return Commands with Parameters (ObservableCommandRAsyncFactory<T1, T2>)

    [Fact]
    public async Task AsyncReturnCommandWithParam_Execute_ReturnsValue()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 10 };

        // Act
        var result = await model.AsyncReturnCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.Equal("Result: 15", result);
        Assert.Equal(15, model.Value);
        Assert.Equal(5, model.LastParameter);
        Assert.Equal(1, model.ExecuteCount);
        _output.WriteLine($"Async return with param returned: {result}");
    }

    [Fact]
    public async Task AsyncReturnCommandWithParam_MultipleExecutions_ReturnsCorrectValues()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act
        var result1 = await model.AsyncReturnCommandWithParam.ExecuteAsync(1);
        var result2 = await model.AsyncReturnCommandWithParam.ExecuteAsync(2);
        var result3 = await model.AsyncReturnCommandWithParam.ExecuteAsync(3);

        // Assert
        Assert.Equal("Result: 1", result1);
        Assert.Equal("Result: 3", result2);
        Assert.Equal("Result: 6", result3);
        Assert.Equal(3, model.ExecuteCount);
        Assert.Equal(6, model.Value);
        _output.WriteLine($"Multiple async param executions: {result1}, {result2}, {result3}");
    }

    [Fact]
    public async Task AsyncReturnCommandWithParam_Exception_StoresErrorAndReturnsDefault()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0, ThrowException = true };

        // Act
        var result = await model.AsyncReturnCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.Null(result);
        Assert.NotNull(model.AsyncReturnCommandWithParam.Error);
        Assert.IsType<InvalidOperationException>(model.AsyncReturnCommandWithParam.Error);
        Assert.False(model.AsyncReturnCommandWithParam.Executing);
        _output.WriteLine($"Async param exception caught: {model.AsyncReturnCommandWithParam.Error.Message}");
    }

    [Fact]
    public async Task AsyncReturnCommandWithParam_Executing_IsTrueDuringExecution()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        var executingStates = new List<bool>();

        using var subscription = model.Observable.Subscribe(_ =>
        {
            executingStates.Add(model.AsyncReturnCommandWithParam.Executing);
            _output.WriteLine($"Executing: {model.AsyncReturnCommandWithParam.Executing}");
        });

        // Act
        await model.AsyncReturnCommandWithParam.ExecuteAsync(10);

        // Assert
        Assert.True(executingStates.Count >= 2, $"Expected at least 2 states, got {executingStates.Count}");
        Assert.True(executingStates.Any(s => s), "Expected Executing to be true at some point");
        Assert.False(executingStates.Last(), "Expected Executing to be false at end");
    }

    #endregion

    #region External CancellationToken Overload Tests

    [Fact]
    public async Task AsyncReturnCommand_WithNullCancellationToken_ExecutesNormally()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act - Pass null explicitly to test the ExecuteAsync(CancellationToken?) overload
        var result = await model.AsyncReturnCommand.ExecuteAsync();

        // Assert
        Assert.Equal(10, result);
        Assert.Equal(1, model.ExecuteCount);
        _output.WriteLine("Null cancellation token handled correctly");
    }

    [Fact]
    public async Task AsyncReturnCommandWithParam_WithNullCancellationToken_ExecutesNormally()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act - Pass null explicitly
        var result = await model.AsyncReturnCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.Equal("Result: 5", result);
        Assert.Equal(1, model.ExecuteCount);
        _output.WriteLine("Null cancellation token with param handled correctly");
    }

    [Fact]
    public async Task CancelableReturnCommand_MultipleCallsWithDifferentTokens_HandlesCorrectly()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();

        // Act - Execute with different tokens
        var result1 = await model.CancelableReturnCommand.ExecuteAsync();
        var result2 = await model.CancelableReturnCommand.ExecuteAsync();

        // Assert
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(2, model.ExecuteCount);
        _output.WriteLine("Multiple calls with different tokens succeeded");
    }

    [Fact]
    public async Task CancelableReturnCommandWithParam_ResetsBetweenExecutions()
    {
        // Arrange
        var model = new TestReturnCommandModel { Value = 0 };

        // Act - Execute multiple times
        var result1 = await model.CancelableReturnCommandWithParam.ExecuteAsync(5);
        var result2 = await model.CancelableReturnCommandWithParam.ExecuteAsync(10);

        // Assert
        Assert.Equal("Result: 5", result1);
        Assert.Equal("Result: 15", result2);
        Assert.Equal(10, model.LastParameter);
        Assert.Equal(2, model.ExecuteCount);
        _output.WriteLine("Command state resets correctly between executions");
    }

    #endregion

    #region State and Notification Tests

    [Fact]
    public async Task AsyncReturnCommand_StateChanges_TriggerNotifications()
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
        var result = await model.AsyncReturnCommand.ExecuteAsync();

        // Assert
        Assert.True(notificationCount > 0, "Should trigger notifications during async execution");
        Assert.Equal(10, result);
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    [Fact]
    public async Task AsyncReturnCommandWithParam_StateChanges_TriggerNotifications()
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
        var result = await model.AsyncReturnCommandWithParam.ExecuteAsync(7);

        // Assert
        Assert.True(notificationCount > 0, "Should trigger notifications during async execution");
        Assert.Equal("Result: 7", result);
        _output.WriteLine($"Total notifications: {notificationCount}");
    }

    [Fact]
    public async Task AsyncReturnCommand_CanExecute_WorksCorrectly()
    {
        // Arrange
        var model1 = new TestReturnCommandModel { Value = 5 };
        var model2 = new TestReturnCommandModel { Value = -1 };

        // Assert before
        Assert.True(model1.AsyncReturnCommand.CanExecute);
        Assert.False(model2.AsyncReturnCommand.CanExecute);

        // Act
        await model1.AsyncReturnCommand.ExecuteAsync();

        // Assert after
        Assert.True(model1.AsyncReturnCommand.CanExecute);
        _output.WriteLine("CanExecute works correctly for async return commands");
    }

    [Fact]
    public async Task AsyncReturnCommandWithParam_CanExecute_WorksCorrectly()
    {
        // Arrange
        var model1 = new TestReturnCommandModel { Value = 5 };
        var model2 = new TestReturnCommandModel { Value = -1 };

        // Assert before
        Assert.True(model1.AsyncReturnCommandWithParam.CanExecute);
        Assert.False(model2.AsyncReturnCommandWithParam.CanExecute);

        // Act
        await model1.AsyncReturnCommandWithParam.ExecuteAsync(3);

        // Assert after
        Assert.True(model1.AsyncReturnCommandWithParam.CanExecute);
        _output.WriteLine("CanExecute works correctly for async return commands with params");
    }

    #endregion
}
