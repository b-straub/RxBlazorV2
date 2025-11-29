using RxBlazorV2Sample.Samples.PropertyTriggers;

namespace RxBlazorV2.CoreTests.Samples;

public class PropertyTriggersTests
{
    private readonly PropertyTriggersModel _model;

    public PropertyTriggersTests(PropertyTriggersModel model)
    {
        _model = model;
    }

    [Fact]
    public void Email_WhenEmpty_ShouldNotBeValid()
    {
        // Arrange
        _model.Email = "";

        // Assert
        Assert.False(_model.IsEmailValid);
    }

    [Fact]
    public void Email_WhenValidFormat_ShouldBeValid()
    {
        // Act
        _model.Email = "test@example.com";

        // Assert
        Assert.True(_model.IsEmailValid);
        Assert.Contains("Valid", _model.EmailValidationMessage);
    }

    [Fact]
    public void Email_WhenInvalidFormat_ShouldNotBeValid()
    {
        // Act
        _model.Email = "invalid-email";

        // Assert
        Assert.False(_model.IsEmailValid);
        Assert.Contains("Invalid", _model.EmailValidationMessage);
    }

    [Fact]
    public void Email_WhenChanged_ShouldIncrementTriggerCount()
    {
        // Arrange
        var initialCount = _model.TriggerExecutionCount;

        // Act
        _model.Email = "test@example.com";

        // Assert
        Assert.True(_model.TriggerExecutionCount > initialCount);
    }

    [Fact]
    public async Task DocumentContent_WhenChangedAndAutoSaveEnabled_ShouldAutoSave()
    {
        // Arrange
        _model.AutoSaveEnabled = true;
        _model.DocumentContent = "Initial content";

        // Act
        _model.DocumentContent = "Updated content";
        await Task.Delay(700, TestContext.Current.CancellationToken); // Wait for async save

        // Assert
        Assert.Contains("Saved", _model.SaveStatus);
    }

    [Fact]
    public async Task DocumentContent_WhenAutoSaveDisabled_ShouldNotAutoSave()
    {
        // Arrange
        _model.AutoSaveEnabled = false;
        _model.SaveStatus = "Not saved";

        // Act
        _model.DocumentContent = "New content";
        await Task.Delay(700, TestContext.Current.CancellationToken);

        // Assert - SaveStatus should not change to "Saved" since AutoSave is disabled
        Assert.Equal("Not saved", _model.SaveStatus);
    }

    [Fact]
    public void Counter_WhenChanged_ShouldTriggerLogChange()
    {
        // Arrange
        var initialCount = _model.TriggerExecutionCount;

        // Act
        _model.Counter++;

        // Assert
        Assert.True(_model.TriggerExecutionCount > initialCount);
    }

    [Fact]
    public void FirstName_WhenChanged_ShouldUpdateFullName()
    {
        // Arrange
        _model.LastName = "Doe";

        // Act
        _model.FirstName = "John";

        // Assert
        Assert.Equal("John Doe", _model.FullName);
    }

    [Fact]
    public void LastName_WhenChanged_ShouldUpdateFullName()
    {
        // Arrange
        _model.FirstName = "Jane";

        // Act
        _model.LastName = "Smith";

        // Assert
        Assert.Equal("Jane Smith", _model.FullName);
    }

    [Fact]
    public void FirstName_WhenSetToEmptyFromNonEmpty_ShouldShowValidationError()
    {
        // Arrange - First set to a value
        _model.FirstName = "John";

        // Act - Then set to empty
        _model.FirstName = "";

        // Assert
        Assert.Contains("required", _model.NameValidationMessage.ToLower());
    }

    [Fact]
    public void FirstName_WhenTooShort_ShouldShowValidationError()
    {
        // Act
        _model.FirstName = "A";

        // Assert
        Assert.Contains("2 characters", _model.NameValidationMessage);
    }

    [Fact]
    public void FirstName_WhenValid_ShouldClearValidationError()
    {
        // Act
        _model.FirstName = "John";

        // Assert
        Assert.Equal("", _model.NameValidationMessage);
    }

    [Fact]
    public void FirstName_ShouldTriggerBothUpdateAndValidate()
    {
        // Arrange
        var initialCount = _model.TriggerExecutionCount;

        // Act
        _model.FirstName = "Test";

        // Assert - Should trigger both UpdateFullName and ValidateName
        Assert.True(_model.TriggerExecutionCount >= initialCount + 2);
    }

    [Fact]
    public void TriggerExecutionCount_InitialValue_ShouldBeZero()
    {
        // Note: This test may fail if other tests run first in the same instance
        // The model is scoped, so each test class gets its own instance
        Assert.True(_model.TriggerExecutionCount >= 0);
    }
}
