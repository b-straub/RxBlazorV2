using Microsoft.Extensions.DependencyInjection;
using ObservableCollections;
using RxBlazorV2Sample.Samples.GenericModels;

namespace RxBlazorV2.CoreTests.Samples;

public class GenericModelsTests
{
    [Fact]
    public void Model_ShouldHaveAccessToBaseModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<GenericModelsBaseModel<string, int>>(sp =>
            new GenericModelsBaseModel<string, int>
            {
                Items = new ObservableList<string>(),
                Values = new ObservableList<int>()
            });
        services.AddScoped<GenericModelsModel<string, int>>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<GenericModelsModel<string, int>>();

        // Act & Assert
        Assert.NotNull(model.Items);
        Assert.NotNull(model.Values);
    }

    [Fact]
    public void AddItemCommand_ShouldAddItemToBaseModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<GenericModelsBaseModel<string, int>>(sp =>
            new GenericModelsBaseModel<string, int>
            {
                Items = new ObservableList<string>(),
                Values = new ObservableList<int>()
            });
        services.AddScoped<GenericModelsModel<string, int>>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<GenericModelsModel<string, int>>();

        // Act
        model.AddItemCommand.Execute("Test Item");

        // Assert
        Assert.Single(model.Items);
        Assert.Contains("Test Item", model.Items);
        Assert.Contains("Total items: 1", model.Status);
    }

    [Fact]
    public void AddItemCommand_WhenMaxReached_CannotExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new ObservableList<string>();
        for (int i = 0; i < 5; i++)
        {
            items.Add($"Item{i}");
        }

        services.AddSingleton<GenericModelsBaseModel<string, int>>(sp =>
            new GenericModelsBaseModel<string, int>
            {
                Items = items,
                Values = new ObservableList<int>()
            });
        services.AddScoped<GenericModelsModel<string, int>>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<GenericModelsModel<string, int>>();

        // Act & Assert
        Assert.False(model.AddItemCommand.CanExecute);
    }

    [Fact]
    public void AddValueCommand_ShouldAddValueToBaseModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<GenericModelsBaseModel<string, int>>(sp =>
            new GenericModelsBaseModel<string, int>
            {
                Items = new ObservableList<string>(),
                Values = new ObservableList<int>()
            });
        services.AddScoped<GenericModelsModel<string, int>>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<GenericModelsModel<string, int>>();

        // Act
        model.AddValueCommand.Execute(42);

        // Assert
        Assert.Single(model.Values);
        Assert.Contains(42, model.Values);
    }

    [Fact]
    public void ClearItemsCommand_ShouldClearAllItems()
    {
        // Arrange
        var services = new ServiceCollection();
        var items = new ObservableList<string> { "Item1", "Item2" };

        services.AddSingleton<GenericModelsBaseModel<string, int>>(sp =>
            new GenericModelsBaseModel<string, int>
            {
                Items = items,
                Values = new ObservableList<int>()
            });
        services.AddScoped<GenericModelsModel<string, int>>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<GenericModelsModel<string, int>>();

        // Act
        model.ClearItemsCommand.Execute();

        // Assert
        Assert.Empty(model.Items);
        Assert.Contains("cleared", model.Status);
    }

    [Fact]
    public void ClearItemsCommand_WhenNoItems_CannotExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<GenericModelsBaseModel<string, int>>(sp =>
            new GenericModelsBaseModel<string, int>
            {
                Items = new ObservableList<string>(),
                Values = new ObservableList<int>()
            });
        services.AddScoped<GenericModelsModel<string, int>>();
        var provider = services.BuildServiceProvider();

        var model = provider.GetRequiredService<GenericModelsModel<string, int>>();

        // Act & Assert
        Assert.False(model.ClearItemsCommand.CanExecute);
    }
}
