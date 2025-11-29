using RxBlazorV2Sample.Samples.CallbackTriggers;

namespace RxBlazorV2.CoreTests.Samples;

public class CallbackTriggersTests
{
    private readonly CallbackTriggersModel _model;

    public CallbackTriggersTests(CallbackTriggersModel model)
    {
        _model = model;
    }

    [Fact]
    public void CurrentUser_InitialValue_ShouldBeGuest()
    {
        Assert.Equal("Guest", _model.CurrentUser);
    }

    [Fact]
    public void CurrentUser_WhenChanged_ShouldUpdateValue()
    {
        // Act
        _model.CurrentUser = "Admin";

        // Assert
        Assert.Equal("Admin", _model.CurrentUser);
    }

    [Fact]
    public void Settings_InitialValue_ShouldBeEmptyJson()
    {
        Assert.Equal("{}", _model.Settings);
    }

    [Fact]
    public void Settings_WhenChanged_ShouldUpdateValue()
    {
        // Act
        _model.Settings = "{\"theme\": \"dark\"}";

        // Assert
        Assert.Equal("{\"theme\": \"dark\"}", _model.Settings);
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
    public void NotificationCount_InitialValue_ShouldBeZero()
    {
        Assert.Equal(0, _model.NotificationCount);
    }

    [Fact]
    public void NotificationCount_WhenIncremented_ShouldUpdateValue()
    {
        // Arrange
        var initialValue = _model.NotificationCount;

        // Act
        _model.NotificationCount++;

        // Assert
        Assert.Equal(initialValue + 1, _model.NotificationCount);
    }

    [Fact]
    public void OnCurrentUserChanged_ShouldBeCallable()
    {
        // Arrange
        var callbackExecuted = false;

        // Act
        _model.OnCurrentUserChanged(() => callbackExecuted = true);
        _model.CurrentUser = "NewUser";

        // Assert - callback should be registered and invoked
        Assert.True(callbackExecuted);
    }

    [Fact]
    public async Task OnSettingsChangedAsync_ShouldBeCallable()
    {
        // Arrange
        var callbackExecuted = false;

        // Act
        _model.OnSettingsChangedAsync(async ct =>
        {
            await Task.Delay(10, ct);
            callbackExecuted = true;
        });
        _model.Settings = "{\"new\": true}";
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert - verify callback was executed
        Assert.True(callbackExecuted);
    }

    [Fact]
    public void HandleThemeUpdate_ShouldBeCallable()
    {
        // Arrange
        var callbackExecuted = false;

        // Act
        _model.HandleThemeUpdate(() => callbackExecuted = true);
        _model.Theme = "Auto";

        // Assert - verify callback was executed
        Assert.True(callbackExecuted);
    }

    [Fact]
    public void OnNotificationCountChanged_ShouldBeCallable()
    {
        // Arrange
        var callbackExecuted = false;

        // Act
        _model.OnNotificationCountChanged(() => callbackExecuted = true);
        _model.NotificationCount++;

        // Assert
        Assert.True(callbackExecuted);
    }

    [Fact]
    public async Task OnNotificationCountChangedAsync_ShouldBeCallable()
    {
        // Arrange
        var callbackExecuted = false;

        // Act
        _model.OnNotificationCountChangedAsync(async ct =>
        {
            await Task.Delay(10, ct);
            callbackExecuted = true;
        });
        _model.NotificationCount++;
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(callbackExecuted);
    }

    [Fact]
    public void CallbackResults_InitialValue_ShouldBeEmpty()
    {
        Assert.Empty(_model.CallbackResults);
    }

    [Fact]
    public void Model_ShouldHaveCallbackRegistrationMethods()
    {
        // Verify that the generated callback methods exist
        var modelType = _model.GetType();

        Assert.NotNull(modelType.GetMethod("OnCurrentUserChanged"));
        Assert.NotNull(modelType.GetMethod("OnSettingsChangedAsync"));
        Assert.NotNull(modelType.GetMethod("HandleThemeUpdate"));
        Assert.NotNull(modelType.GetMethod("OnNotificationCountChanged"));
        Assert.NotNull(modelType.GetMethod("OnNotificationCountChangedAsync"));
    }
}
