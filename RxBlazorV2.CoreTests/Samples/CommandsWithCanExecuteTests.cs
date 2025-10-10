using RxBlazorV2Sample.Samples.CommandsWithCanExecute;

namespace RxBlazorV2.CoreTests.Samples;

public class CommandsWithCanExecuteTests
{
    private readonly CommandsWithCanExecuteModel _model;

    public CommandsWithCanExecuteTests(CommandsWithCanExecuteModel model)
    {
        _model = model;
    }

    [Fact]
    public void IncrementCommand_WhenEnabled_CanExecute()
    {
        // Arrange
        _model.IsEnabled = true;
        _model.Counter = 5;

        // Act & Assert
        Assert.True(_model.IncrementCommand.CanExecute);
    }

    [Fact]
    public void IncrementCommand_WhenDisabled_CannotExecute()
    {
        // Arrange
        _model.IsEnabled = false;

        // Act & Assert
        Assert.False(_model.IncrementCommand.CanExecute);
    }

    [Fact]
    public void IncrementCommand_WhenCounterAtMax_CannotExecute()
    {
        // Arrange
        _model.IsEnabled = true;
        _model.Counter = 10;

        // Act & Assert
        Assert.False(_model.IncrementCommand.CanExecute);
    }

    [Fact]
    public void AddValueCommand_WhenCounterBelowMax_CanExecute()
    {
        // Arrange
        _model.IsEnabled = true;
        _model.Counter = 15;

        // Act & Assert
        Assert.True(_model.AddValueCommand.CanExecute);
    }

    [Fact]
    public void AddValueCommand_WhenCounterAtMax_CannotExecute()
    {
        // Arrange
        _model.IsEnabled = true;
        _model.Counter = 20;

        // Act & Assert
        Assert.False(_model.AddValueCommand.CanExecute);
    }

    [Fact]
    public void ResetCommand_WhenCounterIsZero_CannotExecute()
    {
        // Arrange
        _model.Counter = 0;

        // Act & Assert
        Assert.False(_model.ResetCommand.CanExecute);
    }

    [Fact]
    public void ResetCommand_WhenCounterIsPositive_CanExecute()
    {
        // Arrange
        _model.Counter = 5;

        // Act & Assert
        Assert.True(_model.ResetCommand.CanExecute);
    }

    [Fact]
    public void ResetCommand_ShouldResetCounter()
    {
        // Arrange
        _model.Counter = 10;

        // Act
        _model.ResetCommand.Execute();

        // Assert
        Assert.Equal(0, _model.Counter);
        Assert.Contains("Counter reset", _model.Message);
    }

    [Fact]
    public void ToggleEnabledCommand_ShouldToggleIsEnabled()
    {
        // Arrange
        _model.IsEnabled = true;

        // Act
        _model.ToggleEnabledCommand.Execute();

        // Assert
        Assert.False(_model.IsEnabled);
        Assert.Contains("disabled", _model.Message);
    }
}
