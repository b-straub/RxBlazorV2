using RxBlazorV2Sample.Samples.BasicCommands;

namespace RxBlazorV2.CoreTests.Samples;

public class CommandsWithReturnValueTests
{
    private readonly BasicCommandsRModel _model;

    public CommandsWithReturnValueTests(BasicCommandsRModel model)
    {
        _model = model;
    }

    [Fact]
    public void IncrementCommand_ShouldReturnValue()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result = _model.IncrementCommand.Execute();

        // Assert
        Assert.Equal(10, result);
        Assert.Equal(1, _model.Counter);
    }

    [Fact]
    public void IncrementCommand_ShouldIncrementCounterAndLog()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        _model.IncrementCommand.Execute();

        // Assert
        Assert.Equal(1, _model.Counter);
        Assert.Contains(_model.LogEntries, le => le.Message == "Sync command executed");
    }

    [Fact]
    public async Task IncrementAsyncCommand_ShouldReturnValue()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result = await _model.IncrementAsyncCommand.ExecuteAsync();

        // Assert
        Assert.Equal(10, result);
        Assert.Equal(1, _model.Counter);
    }

    [Fact]
    public async Task IncrementAsyncCommand_ShouldIncrementCounterAndLog()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        await _model.IncrementAsyncCommand.ExecuteAsync();

        // Assert
        Assert.Equal(1, _model.Counter);
        Assert.Contains(_model.LogEntries, le => le.Message == "Async command completed");
    }

    [Fact]
    public void IncrementCommand_MultipleCalls_ShouldReturnCorrectValues()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result1 = _model.IncrementCommand.Execute();
        var result2 = _model.IncrementCommand.Execute();
        var result3 = _model.IncrementCommand.Execute();

        // Assert
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(30, result3);
        Assert.Equal(3, _model.Counter);
    }

    [Fact]
    public async Task IncrementAsyncCommand_MultipleCalls_ShouldReturnCorrectValues()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result1 = await _model.IncrementAsyncCommand.ExecuteAsync();
        var result2 = await _model.IncrementAsyncCommand.ExecuteAsync();
        var result3 = await _model.IncrementAsyncCommand.ExecuteAsync();

        // Assert
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(30, result3);
        Assert.Equal(3, _model.Counter);
    }

    [Fact]
    public async Task IncrementAsyncCommand_ShouldSetExecutingFlag()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var task = _model.IncrementAsyncCommand.ExecuteAsync();
        var executingDuringRun = _model.IncrementAsyncCommand.Executing;
        await task;
        var executingAfterComplete = _model.IncrementAsyncCommand.Executing;

        // Assert
        Assert.True(executingDuringRun);
        Assert.False(executingAfterComplete);
    }

    [Fact]
    public void IncrementCommand_ShouldReturnNullableValue()
    {
        // Arrange
        _model.Counter = 4;

        // Act
        var result = _model.IncrementCommand.Execute();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Value);
        Assert.Equal(5, _model.Counter);
    }

    [Fact]
    public async Task IncrementAsyncCommand_ShouldReturnNullableValue()
    {
        // Arrange
        _model.Counter = 4;

        // Act
        var result = await _model.IncrementAsyncCommand.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Value);
        Assert.Equal(5, _model.Counter);
    }
}
