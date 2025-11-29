using RxBlazorV2Sample.Samples.ComponentTriggers;

namespace RxBlazorV2.CoreTests.Samples;

public class ComponentTriggersTests
{
    private readonly ComponentTriggersModel _model;

    public ComponentTriggersTests(ComponentTriggersModel model)
    {
        _model = model;
    }

    [Fact]
    public void Theme_InitialValue_ShouldBeLight()
    {
        Assert.Equal("Light", _model.Theme);
    }

    [Fact]
    public void Theme_WhenChanged_ShouldUpdateValue()
    {
        // Act
        _model.Theme = "Dark";

        // Assert
        Assert.Equal("Dark", _model.Theme);
    }

    [Fact]
    public void UserName_InitialValue_ShouldBeEmpty()
    {
        Assert.Equal("", _model.UserName);
    }

    [Fact]
    public void UserName_WhenChanged_ShouldUpdateValue()
    {
        // Act
        _model.UserName = "TestUser";

        // Assert
        Assert.Equal("TestUser", _model.UserName);
    }

    [Fact]
    public void RenderOnlyCounter_WhenIncremented_ShouldUpdateValue()
    {
        // Arrange
        var initialValue = _model.RenderOnlyCounter;

        // Act
        _model.RenderOnlyCounter++;

        // Assert
        Assert.Equal(initialValue + 1, _model.RenderOnlyCounter);
    }

    [Fact]
    public void BackgroundStatus_InitialValue_ShouldBeIdle()
    {
        Assert.Equal("Idle", _model.BackgroundStatus);
    }

    [Fact]
    public void BackgroundStatus_WhenChanged_ShouldUpdateValue()
    {
        // Act
        _model.BackgroundStatus = "Running";

        // Assert
        Assert.Equal("Running", _model.BackgroundStatus);
    }

    [Fact]
    public void ThemeHookCount_InitialValue_ShouldBeZero()
    {
        Assert.Equal(0, _model.ThemeHookCount);
    }

    [Fact]
    public void UserNameHookCount_InitialValue_ShouldBeZero()
    {
        Assert.Equal(0, _model.UserNameHookCount);
    }

    [Fact]
    public void BackgroundHookCount_InitialValue_ShouldBeZero()
    {
        Assert.Equal(0, _model.BackgroundHookCount);
    }

    [Fact]
    public void LastHookMessage_InitialValue_ShouldIndicateNoHooksExecuted()
    {
        Assert.Contains("No hooks executed", _model.LastHookMessage);
    }

    [Fact]
    public void Model_ShouldHaveComponentTriggerAttributes()
    {
        // Verify the model class has ObservableComponent attribute by checking generated component exists
        var modelType = _model.GetType();
        var componentType = modelType.Assembly.GetType($"{modelType.Namespace}.{modelType.Name}Component");
        Assert.NotNull(componentType);
    }
}
