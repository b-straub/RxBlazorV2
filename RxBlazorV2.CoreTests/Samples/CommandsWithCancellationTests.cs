using RxBlazorV2Sample.Samples.CommandsWithCancellation;

namespace RxBlazorV2.CoreTests.Samples;

public class CommandsWithCancellationTests
{
    private readonly CommandsWithCancellationModel _model;

    public CommandsWithCancellationTests(CommandsWithCancellationModel model)
    {
        _model = model;
    }

    [Fact]
    public async Task LongOperationCommand_ShouldCompleteSuccessfully()
    {
        // Act
        await _model.LongOperationCommand.ExecuteAsync();

        // Assert
        Assert.Equal(100, _model.Progress);
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("completed successfully"));
    }

    [Fact]
    public async Task LongOperationCommand_WhenCancelled_ShouldStopAndResetProgress()
    {
        // Act
        var task = _model.LongOperationCommand.ExecuteAsync();
        await Task.Delay(1000, TestContext.Current.CancellationToken); // Let it run a bit
        _model.LongOperationCommand.Cancel();

        try
        {
            await task;
        }
        catch
        {
            // Expected
        }

        // Assert
        Assert.Equal(0, _model.Progress);
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("cancelled"));
    }

    [Fact]
    public async Task LongOperationWithParamCommand_ShouldCompleteWithCorrectIterations()
    {
        // Act
        await _model.LongOperationWithParamCommand.ExecuteAsync(3);

        // Assert
        Assert.Equal(100, _model.Progress);
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("Completed all 3 iterations"));
    }

    [Fact]
    public async Task LongOperationWithParamCommand_WhenCancelled_ShouldStopEarly()
    {
        // Act
        var task = _model.LongOperationWithParamCommand.ExecuteAsync(10);
        await Task.Delay(1500, TestContext.Current.CancellationToken); // Let it run a bit
        _model.LongOperationWithParamCommand.Cancel();

        try
        {
            await task;
        }
        catch
        {
            // Expected
        }

        // Assert
        Assert.Equal(0, _model.Progress);
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("cancelled"));
    }

    [Fact]
    public async Task Commands_ShouldSetExecutingFlagDuringExecution()
    {
        // Act
        var task = _model.LongOperationCommand.ExecuteAsync();
        var executingDuringRun = _model.LongOperationCommand.Executing;
        await task;
        var executingAfterComplete = _model.LongOperationCommand.Executing;

        // Assert
        Assert.True(executingDuringRun);
        Assert.False(executingAfterComplete);
    }
}
