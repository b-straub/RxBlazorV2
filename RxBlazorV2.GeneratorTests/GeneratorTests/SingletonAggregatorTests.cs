extern alias xunitv3;
using RxBlazorV2Generator.Generators.Templates;
using RxBlazorV2Generator.Models;
using Assert = xunitv3::Xunit.Assert;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for SingletonAggregatorTemplate, specifically the name collision disambiguation logic.
/// </summary>
public class SingletonAggregatorTests
{
    [Fact]
    public void GenerateAggregationModel_WithDuplicatePropertyNames_DisambiguatesUsingNamespace()
    {
        // Arrange - two models with the same class name but different namespaces
        // This simulates the scenario where both ReactivePatternSample.Status.Models.StatusModel
        // and RxBlazorV2.MudBlazor.Components.StatusModel exist
        var singletons = new List<SingletonModelInfo>
        {
            new("Namespace1.Models.StatusModel", "StatusModel", "Namespace1.Models", false),
            new("Namespace2.Components.StatusModel", "StatusModel", "Namespace2.Components", false)
        };

        // Act
        var generatedCode = SingletonAggregatorTemplate.GenerateAggregationModel(singletons, "TestApp");

        // Assert - property names should be disambiguated using last namespace segment
        // "StatusModel" from "Namespace1.Models" -> "ModelsStatus"
        // "StatusModel" from "Namespace2.Components" -> "ComponentsStatus"
        Assert.Contains("public Namespace1.Models.StatusModel ModelsStatus { get; }", generatedCode);
        Assert.Contains("public Namespace2.Components.StatusModel ComponentsStatus { get; }", generatedCode);

        // Constructor parameters should also be disambiguated
        Assert.Contains("Namespace1.Models.StatusModel modelsStatus", generatedCode);
        Assert.Contains("Namespace2.Components.StatusModel componentsStatus", generatedCode);

        // Should NOT contain duplicate "Status" property names
        Assert.DoesNotContain("public Namespace1.Models.StatusModel Status { get; }", generatedCode);
        Assert.DoesNotContain("public Namespace2.Components.StatusModel Status { get; }", generatedCode);
    }

    [Fact]
    public void GenerateAggregationModel_WithUniquePropertyNames_KeepsOriginalNames()
    {
        // Arrange - models with different class names (no collision)
        var singletons = new List<SingletonModelInfo>
        {
            new("App.Models.AuthModel", "AuthModel", "App.Models", false),
            new("App.Models.SettingsModel", "SettingsModel", "App.Models", false),
            new("App.Models.TodoModel", "TodoModel", "App.Models", false)
        };

        // Act
        var generatedCode = SingletonAggregatorTemplate.GenerateAggregationModel(singletons, "TestApp");

        // Assert - original property names should be preserved (no disambiguation needed)
        Assert.Contains("public App.Models.AuthModel Auth { get; }", generatedCode);
        Assert.Contains("public App.Models.SettingsModel Settings { get; }", generatedCode);
        Assert.Contains("public App.Models.TodoModel Todo { get; }", generatedCode);

        // Constructor parameters should use original names
        Assert.Contains("App.Models.AuthModel auth", generatedCode);
        Assert.Contains("App.Models.SettingsModel settings", generatedCode);
        Assert.Contains("App.Models.TodoModel todo", generatedCode);
    }

    [Fact]
    public void GenerateAggregationModel_WithMixedCollisions_OnlyDisambiguatesDuplicates()
    {
        // Arrange - some models with same name, some unique
        var singletons = new List<SingletonModelInfo>
        {
            new("App.Auth.AuthModel", "AuthModel", "App.Auth", false),
            new("App.Status.StatusModel", "StatusModel", "App.Status", false),
            new("External.Components.StatusModel", "StatusModel", "External.Components", false)
        };

        // Act
        var generatedCode = SingletonAggregatorTemplate.GenerateAggregationModel(singletons, "TestApp");

        // Assert - AuthModel should keep original name (no collision)
        Assert.Contains("public App.Auth.AuthModel Auth { get; }", generatedCode);

        // StatusModel duplicates should be disambiguated
        Assert.Contains("public App.Status.StatusModel StatusStatus { get; }", generatedCode);
        Assert.Contains("public External.Components.StatusModel ComponentsStatus { get; }", generatedCode);
    }

    [Fact]
    public void GenerateAggregationModel_WithThreeWayCollision_DisambiguatesAll()
    {
        // Arrange - three models with the same class name
        var singletons = new List<SingletonModelInfo>
        {
            new("App.A.ConfigModel", "ConfigModel", "App.A", false),
            new("App.B.ConfigModel", "ConfigModel", "App.B", false),
            new("App.C.ConfigModel", "ConfigModel", "App.C", false)
        };

        // Act
        var generatedCode = SingletonAggregatorTemplate.GenerateAggregationModel(singletons, "TestApp");

        // Assert - all three should be disambiguated
        Assert.Contains("public App.A.ConfigModel AConfig { get; }", generatedCode);
        Assert.Contains("public App.B.ConfigModel BConfig { get; }", generatedCode);
        Assert.Contains("public App.C.ConfigModel CConfig { get; }", generatedCode);

        // Should NOT contain any plain "Config" property
        Assert.DoesNotContain("ConfigModel Config { get; }", generatedCode);
    }

    [Fact]
    public void GenerateAggregationModel_EmptyList_GeneratesEmptyModel()
    {
        // Arrange
        var singletons = new List<SingletonModelInfo>();

        // Act
        var generatedCode = SingletonAggregatorTemplate.GenerateAggregationModel(singletons, "TestApp");

        // Assert - should still generate a valid model class
        Assert.Contains("public partial class RxBlazorV2LayoutModel : ObservableModel", generatedCode);
        Assert.Contains("namespace TestApp.Layout;", generatedCode);
    }
}
