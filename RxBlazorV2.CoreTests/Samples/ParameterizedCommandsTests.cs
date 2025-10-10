using RxBlazorV2Sample.Samples.ParameterizedCommands;

namespace RxBlazorV2.CoreTests.Samples;

public class ParameterizedCommandsTests
{
    private readonly ParameterizedCommandsModel _model;

    public ParameterizedCommandsTests(ParameterizedCommandsModel model)
    {
        _model = model;
    }

    [Fact]
    public void AddCommand_ShouldAddValueToCounter()
    {
        // Act
        _model.AddCommand.Execute(5);

        // Assert
        Assert.Equal(5, _model.Counter);
        Assert.Contains("Added 5 synchronously", _model.LogEntries.Last().Message);
    }

    [Fact]
    public async Task AddAsyncCommand_ShouldAddValueToCounter()
    {
        // Act
        await _model.AddAsyncCommand.ExecuteAsync(10);

        // Assert
        Assert.Equal(10, _model.Counter);
        Assert.Contains("Added 10 asynchronously", _model.LogEntries.Last().Message);
    }

    [Fact]
    public void SetMessageCommand_ShouldUpdateLastOperation()
    {
        // Act
        _model.SetMessageCommand.Execute("TestMessage");

        // Assert
        Assert.Contains("Message set: 'TestMessage'", _model.LogEntries.Last().Message);
    }

    [Fact]
    public void AddCommand_MultipleCalls_ShouldAccumulate()
    {
        // Act
        _model.AddCommand.Execute(5);
        _model.AddCommand.Execute(10);
        _model.AddCommand.Execute(3);

        // Assert
        Assert.Equal(18, _model.Counter);
    }
}
