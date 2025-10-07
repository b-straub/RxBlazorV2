using RxBlazorV2.CoreTests.TestFixtures;
using Xunit.Abstractions;

namespace RxBlazorV2.CoreTests;

public class ObservableCommandAsyncTests
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandAsyncTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task AsyncCommand_ExecuteAsync_UpdatesValue()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        await model.AsyncCommand.ExecuteAsync();

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(1, model.Value);
    }

    [Fact]
    public async Task AsyncCommand_Executing_IsTrueDuringExecution()
    {
        // Arrange
        var model = new TestCommandModel();
        var executingStates = new List<bool>();

        using var subscription = model.Observable.Subscribe(properties =>
        {
            executingStates.Add(model.AsyncCommand.Executing);
            _output.WriteLine($"Executing: {model.AsyncCommand.Executing}, Properties: {string.Join(", ", properties)}");
        });

        // Act
        await model.AsyncCommand.ExecuteAsync();

        // Assert
        Assert.True(executingStates.Count >= 2, $"Expected at least 2 notifications, got {executingStates.Count}");
        Assert.True(executingStates.Any(s => s), "Expected Executing to be true at some point");
        Assert.False(executingStates.Last(), "Expected Executing to be false at end");
    }

    [Fact]
    public async Task AsyncCommand_CanExecute_WhenConditionIsTrue()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };

        // Act & Assert
        Assert.True(model.AsyncCommand.CanExecute);
        await model.AsyncCommand.ExecuteAsync();
        Assert.True(model.AsyncCommand.CanExecute);
    }

    [Fact]
    public void AsyncCommand_CanExecute_WhenConditionIsFalse()
    {
        // Arrange
        var model = new TestCommandModel { Value = -1 };

        // Assert
        Assert.False(model.AsyncCommand.CanExecute);
    }

    [Fact]
    public async Task AsyncCommand_ExecuteAsync_NotifiesModel()
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
        await model.AsyncCommand.ExecuteAsync();

        // Assert - notifications during async execution
        Assert.True(notificationCount >= 2, $"Expected at least 2 notifications, got {notificationCount}");
    }

    [Fact]
    public async Task AsyncCommand_ExecuteAsync_CatchesException()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };

        // Act
        await model.AsyncCommand.ExecuteAsync();

        // Assert
        Assert.NotNull(model.AsyncCommand.Error);
        Assert.IsType<InvalidOperationException>(model.AsyncCommand.Error);
        Assert.Equal("Test exception", model.AsyncCommand.Error.Message);
        Assert.False(model.AsyncCommand.Executing);
        _output.WriteLine($"Exception caught: {model.AsyncCommand.Error.Message}");
    }

    [Fact]
    public async Task AsyncCommand_ResetError_ClearsError()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };
        await model.AsyncCommand.ExecuteAsync();
        Assert.NotNull(model.AsyncCommand.Error);

        // Act
        model.AsyncCommand.ResetError();

        // Assert
        Assert.Null(model.AsyncCommand.Error);
    }

    [Fact]
    public async Task AsyncCommandWithParam_ExecuteAsync_UpdatesValueWithParameter()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        await model.AsyncCommandWithParam.ExecuteAsync(5);

        // Assert
        Assert.Equal(1, model.ExecuteCount);
        Assert.Equal(5, model.LastParameter);
        Assert.Equal(5, model.Value);
    }

    [Fact]
    public async Task AsyncCommandWithParam_ExecuteAsync_MultipleParameters()
    {
        // Arrange
        var model = new TestCommandModel();

        // Act
        await model.AsyncCommandWithParam.ExecuteAsync(3);
        await model.AsyncCommandWithParam.ExecuteAsync(7);

        // Assert
        Assert.Equal(2, model.ExecuteCount);
        Assert.Equal(7, model.LastParameter);
        Assert.Equal(10, model.Value);
    }

    [Fact]
    public async Task AsyncCommandWithParam_Executing_IsTrueDuringExecution()
    {
        // Arrange
        var model = new TestCommandModel();
        var executingStates = new List<bool>();

        using var subscription = model.Observable.Subscribe(properties =>
        {
            executingStates.Add(model.AsyncCommandWithParam.Executing);
            _output.WriteLine($"Executing: {model.AsyncCommandWithParam.Executing}");
        });

        // Act
        await model.AsyncCommandWithParam.ExecuteAsync(10);

        // Assert
        Assert.True(executingStates.Count >= 2, $"Expected at least 2 states, got {executingStates.Count}");
        Assert.True(executingStates.Any(s => s), "Expected Executing to be true at some point");
        Assert.False(model.AsyncCommandWithParam.Executing, "Expected Executing to be false at end");
    }

    [Fact]
    public void AsyncCommandWithParam_CanExecute_WhenConditionIsTrue()
    {
        // Arrange
        var model = new TestCommandModel { Value = 5 };

        // Assert
        Assert.True(model.AsyncCommandWithParam.CanExecute);
    }

    [Fact]
    public void AsyncCommandWithParam_CanExecute_WhenConditionIsFalse()
    {
        // Arrange
        var model = new TestCommandModel { Value = -1 };

        // Assert
        Assert.False(model.AsyncCommandWithParam.CanExecute);
    }

    [Fact]
    public async Task AsyncCommandWithParam_ExecuteAsync_CatchesException()
    {
        // Arrange
        var model = new TestCommandModel { ThrowException = true };

        // Act
        await model.AsyncCommandWithParam.ExecuteAsync(10);

        // Assert
        Assert.NotNull(model.AsyncCommandWithParam.Error);
        Assert.IsType<InvalidOperationException>(model.AsyncCommandWithParam.Error);
        Assert.False(model.AsyncCommandWithParam.Executing);
    }

    [Fact]
    public void AsyncCommand_PropertyChange_TriggersModelObservable()
    {
        // Arrange
        var model = new TestCommandModel();
        var notificationCount = 0;

        using var subscription = model.Observable.Subscribe(_ => notificationCount++);

        // Act
        model.Value = 10;

        // Assert
        Assert.Equal(1, notificationCount);
        _output.WriteLine("Model observable triggered on property change");
    }
}
