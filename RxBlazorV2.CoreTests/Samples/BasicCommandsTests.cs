using RxBlazorV2Sample.Samples.BasicCommands;

namespace RxBlazorV2.CoreTests.Samples;

public class BasicCommandsTests
{
    private readonly BasicCommandsModel _model;

    public BasicCommandsTests(BasicCommandsModel model)
    {
        _model = model;
    }

    [Fact]
    public void IncrementCommand_ShouldIncrementCounter()
    {
        // Act
        _model.Counter = 0;
        _model.IncrementCommand.Execute();

        // Assert
        Assert.Equal(1, _model.Counter);
        Assert.Contains(_model.LogEntries, le => le.Message == "Sync command executed");
    }

    [Fact]
    public async Task IncrementAsyncCommand_ShouldIncrementCounter()
    {
        // Act
        _model.Counter = 0;
        await _model.IncrementAsyncCommand.ExecuteAsync();

        // Assert
        Assert.Equal(1, _model.Counter);
        Assert.Contains(_model.LogEntries, le => le.Message == "Async command completed");
    }

    [Fact]
    public void IncrementCommand_MultipleCalls_ShouldIncrementMultipleTimes()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        _model.IncrementCommand.Execute();
        _model.IncrementCommand.Execute();
        _model.IncrementCommand.Execute();

        // Assert
        Assert.Equal(3, _model.Counter);
    }

    [Fact]
    public async Task IncrementAsyncCommand_ShouldSetExecutingFlag()
    {
        // Act
        var task = _model.IncrementAsyncCommand.ExecuteAsync();
        var executingDuringRun = _model.IncrementAsyncCommand.Executing;
        await task;
        var executingAfterComplete = _model.IncrementAsyncCommand.Executing;

        // Assert
        Assert.True(executingDuringRun);
        Assert.False(executingAfterComplete);
    }
}
