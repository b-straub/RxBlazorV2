using RxBlazorV2Sample.Samples.CrossComponentCommunication;

namespace RxBlazorV2.CoreTests.Samples;

public class CrossComponentCommunicationTests
{
    private readonly CrossComponentCommunicationModel _model;

    public CrossComponentCommunicationTests(CrossComponentCommunicationModel model)
    {
        _model = model;
    }

    [Fact]
    public void IncrementCommand_ShouldIncrementSharedCounter()
    {
        // Act
        _model.SharedCounter = 0;
        _model.IncrementCommand.Execute();

        // Assert
        Assert.Equal(1, _model.SharedCounter);
    }

    [Fact]
    public void DecrementCommand_WhenCounterIsZero_CannotExecute()
    {
        // Arrange
        _model.SharedCounter = 0;

        // Act & Assert
        Assert.False(_model.DecrementCommand.CanExecute);
    }

    [Fact]
    public void DecrementCommand_WhenCounterIsPositive_CanExecute()
    {
        // Arrange
        _model.SharedCounter = 5;

        // Act & Assert
        Assert.True(_model.DecrementCommand.CanExecute);
    }

    [Fact]
    public void DecrementCommand_ShouldDecrementSharedCounter()
    {
        // Arrange
        _model.SharedCounter = 5;

        // Act
        _model.DecrementCommand.Execute();

        // Assert
        Assert.Equal(4, _model.SharedCounter);
    }

    [Fact]
    public void ResetCommand_ShouldResetCounterToZero()
    {
        // Arrange
        _model.SharedCounter = 10;

        // Act
        _model.ResetCommand.Execute();

        // Assert
        Assert.Equal(0, _model.SharedCounter);
        Assert.Contains("reset", _model.SharedMessage);
    }

    [Fact]
    public void ToggleActiveCommand_ShouldToggleIsActive()
    {
        // Arrange
        _model.IsActive = true;

        // Act
        _model.ToggleActiveCommand.Execute();

        // Assert
        Assert.False(_model.IsActive);
        Assert.Contains("inactive", _model.SharedMessage);
    }

    [Fact]
    public void ToggleActiveCommand_ShouldUpdateMessage()
    {
        // Arrange
        _model.IsActive = false;

        // Act
        _model.ToggleActiveCommand.Execute();

        // Assert
        Assert.Contains("active", _model.SharedMessage);
    }

    [Fact]
    public void SharedProperties_ShouldBeAccessibleFromAllComponents()
    {
        // Arrange
        _model.SharedCounter = 42;
        _model.SharedMessage = "Test Message";
        _model.IsActive = true;

        // Act & Assert - All properties should be accessible
        Assert.Equal(42, _model.SharedCounter);
        Assert.Equal("Test Message", _model.SharedMessage);
        Assert.True(_model.IsActive);
    }

    [Fact]
    public void MultipleCommands_ShouldWorkSequentially()
    {
        // Act
        _model.IncrementCommand.Execute();
        _model.IncrementCommand.Execute();
        _model.IncrementCommand.Execute();
        _model.DecrementCommand.Execute();

        // Assert
        Assert.Equal(2, _model.SharedCounter);
    }

    [Fact]
    public void SharedMessage_ShouldPersistAcrossOperations()
    {
        // Arrange
        _model.SharedMessage = "Custom Message";

        // Act
        _model.IncrementCommand.Execute();

        // Assert - Message should remain unless explicitly changed
        Assert.Equal("Custom Message", _model.SharedMessage);
    }
}
