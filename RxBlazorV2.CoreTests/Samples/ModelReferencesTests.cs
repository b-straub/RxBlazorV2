using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2Sample.Samples.ModelReferences;

namespace RxBlazorV2.CoreTests.Samples;

public class ModelReferencesTests
{
    [Fact]
    public void Model_ShouldHaveAccessToSharedModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ModelReferencesSharedModel>();
        services.AddScoped<ModelReferencesModel>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<ModelReferencesModel>();

        // Act & Assert - Model should have access to shared model through injection
        Assert.NotNull(model);
    }

    [Fact]
    public void SendNotificationCommand_WhenNotificationsEnabled_CanExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ModelReferencesSharedModel>();
        services.AddScoped<ModelReferencesModel>();
        var provider = services.BuildServiceProvider();

        var sharedModel = provider.GetRequiredService<ModelReferencesSharedModel>();
        var model = provider.GetRequiredService<ModelReferencesModel>();
        sharedModel.NotificationsEnabled = true;

        // Act & Assert
        Assert.True(model.SendNotificationCommand.CanExecute);
    }

    [Fact]
    public void SendNotificationCommand_WhenNotificationsDisabled_CannotExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ModelReferencesSharedModel>();
        services.AddScoped<ModelReferencesModel>();
        var provider = services.BuildServiceProvider();

        var sharedModel = provider.GetRequiredService<ModelReferencesSharedModel>();
        var model = provider.GetRequiredService<ModelReferencesModel>();
        sharedModel.NotificationsEnabled = false;

        // Act & Assert
        Assert.False(model.SendNotificationCommand.CanExecute);
    }

    [Fact]
    public void SendNotificationCommand_ShouldIncrementNotificationCount()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ModelReferencesSharedModel>();
        services.AddScoped<ModelReferencesModel>();
        var provider = services.BuildServiceProvider();

        var sharedModel = provider.GetRequiredService<ModelReferencesSharedModel>();
        var model = provider.GetRequiredService<ModelReferencesModel>();
        sharedModel.NotificationsEnabled = true;

        // Act
        model.SendNotificationCommand.Execute();

        // Assert
        Assert.Equal(1, model.NotificationCount);
        Assert.Contains("Notification #1 sent", model.Message);
    }

    [Fact]
    public void UpdateMessageCommand_ShouldIncludeSharedModelProperties()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ModelReferencesSharedModel>();
        services.AddScoped<ModelReferencesModel>();
        var provider = services.BuildServiceProvider();

        var sharedModel = provider.GetRequiredService<ModelReferencesSharedModel>();
        var model = provider.GetRequiredService<ModelReferencesModel>();
        sharedModel.Theme = "Dark";
        sharedModel.Language = "German";

        // Act
        model.UpdateMessageCommand.Execute();

        // Assert
        Assert.Contains("Dark", model.Message);
        Assert.Contains("German", model.Message);
    }

    [Fact]
    public void SharedModel_ToggleThemeCommand_ShouldToggleTheme()
    {
        // Arrange
        var sharedModel = new ModelReferencesSharedModel { Theme = "Light" };

        // Act
        sharedModel.ToggleThemeCommand.Execute();

        // Assert
        Assert.Equal("Dark", sharedModel.Theme);
    }

    [Fact]
    public void SharedModel_ToggleNotificationsCommand_ShouldToggleNotifications()
    {
        // Arrange
        var sharedModel = new ModelReferencesSharedModel { NotificationsEnabled = true };

        // Act
        sharedModel.ToggleNotificationsCommand.Execute();

        // Assert
        Assert.False(sharedModel.NotificationsEnabled);
    }
}
