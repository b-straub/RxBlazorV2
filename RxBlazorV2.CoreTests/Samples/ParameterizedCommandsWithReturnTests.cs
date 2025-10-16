using RxBlazorV2Sample.Samples.ParameterizedCommandsWithReturn;

namespace RxBlazorV2.CoreTests.Samples;

public class ParameterizedCommandsWithReturnTests
{
    private readonly ParameterizedCommandsRModel _model;

    public ParameterizedCommandsWithReturnTests(ParameterizedCommandsRModel model)
    {
        _model = model;
    }

    [Fact]
    public void CalculateCommand_ShouldReturnCorrectValue()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result = _model.CalculateCommand.Execute(10);

        // Assert
        Assert.Equal(15.0, result);
        Assert.Equal(10, _model.Counter);
    }

    [Fact]
    public async Task CalculateAsyncCommand_ShouldReturnCorrectValue()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result = await _model.CalculateAsyncCommand.ExecuteAsync(10);

        // Assert
        Assert.Equal(15.0, result);
        Assert.Equal(10, _model.Counter);
    }

    [Fact]
    public void CalculateCommand_ShouldAccumulateCounter()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result1 = _model.CalculateCommand.Execute(5);
        var result2 = _model.CalculateCommand.Execute(5);
        var result3 = _model.CalculateCommand.Execute(10);

        // Assert
        Assert.Equal(7.5, result1);   // 5 * 1.5
        Assert.Equal(15.0, result2);  // 10 * 1.5
        Assert.Equal(30.0, result3);  // 20 * 1.5
        Assert.Equal(20, _model.Counter);
    }

    [Fact]
    public async Task CalculateAsyncCommand_ShouldAccumulateCounter()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        var result1 = await _model.CalculateAsyncCommand.ExecuteAsync(5);
        var result2 = await _model.CalculateAsyncCommand.ExecuteAsync(5);
        var result3 = await _model.CalculateAsyncCommand.ExecuteAsync(10);

        // Assert
        Assert.Equal(7.5, result1);   // 5 * 1.5
        Assert.Equal(15.0, result2);  // 10 * 1.5
        Assert.Equal(30.0, result3);  // 20 * 1.5
        Assert.Equal(20, _model.Counter);
    }

    [Fact]
    public void FormatCommand_ShouldReturnFormattedString()
    {
        // Act
        var result = _model.FormatCommand.Execute("hello world");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("HELLO WORLD", result);
        Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\] HELLO WORLD", result);
    }

    [Fact]
    public async Task FormatAsyncCommand_ShouldReturnFormattedString()
    {
        // Act
        var result = await _model.FormatAsyncCommand.ExecuteAsync("hello world");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("HELLO WORLD", result);
        Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\] HELLO WORLD", result);
    }

    [Fact]
    public void FormatCommand_MultipleCalls_ShouldReturnDifferentTimestamps()
    {
        // Act
        var result1 = _model.FormatCommand.Execute("test1");
        var result2 = _model.FormatCommand.Execute("test2");

        // Assert
        Assert.Contains("TEST1", result1);
        Assert.Contains("TEST2", result2);
    }

    [Fact]
    public async Task FormatAsyncCommand_ShouldSetExecutingFlag()
    {
        // Act
        var task = _model.FormatAsyncCommand.ExecuteAsync("test");
        var executingDuringRun = _model.FormatAsyncCommand.Executing;
        await task;
        var executingAfterComplete = _model.FormatAsyncCommand.Executing;

        // Assert
        Assert.True(executingDuringRun);
        Assert.False(executingAfterComplete);
    }

    [Fact]
    public void CalculateCommand_ShouldLogExecution()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        _model.CalculateCommand.Execute(5);

        // Assert
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("Calculated 5 * 1.5"));
    }

    [Fact]
    public async Task CalculateAsyncCommand_ShouldLogExecution()
    {
        // Arrange
        _model.Counter = 0;

        // Act
        await _model.CalculateAsyncCommand.ExecuteAsync(5);

        // Assert
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("Calculated 5 * 1.5"));
    }

    [Fact]
    public void FormatCommand_ShouldLogExecution()
    {
        // Act
        _model.FormatCommand.Execute("test");

        // Assert
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("Formatted message synchronously"));
    }

    [Fact]
    public async Task FormatAsyncCommand_ShouldLogExecution()
    {
        // Act
        await _model.FormatAsyncCommand.ExecuteAsync("test");

        // Assert
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("Formatted message asynchronously"));
    }
}
