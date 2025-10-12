using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;
using Xunit;

namespace RxBlazorV2.CoreTests;

public class InjectedModelTests : Bunit.TestContext
{
    public InjectedModelTests()
    {
        Services.AddSingleton<TestInjectedModel>();
        Services.AddSingleton<TestExtraInjectedModel>();
    }

    [Fact]
    public void InjectPropertyComponent_SubscribesToModelPropertyA()
    {
        // Arrange
        var model = Services.GetRequiredService<TestInjectedModel>();
        var cut = RenderComponent<TestInjectPropertyComponent>();

        // Wait for component to initialize
        cut.WaitForState(() => cut.Markup.Contains("Injected A"), timeout: TimeSpan.FromSeconds(2));
        var initialText = cut.Find(".property-a").TextContent;

        // Act
        model.InjectedPropertyA = "Updated A";

        // Wait for component to update
        cut.WaitForState(() => cut.Find(".property-a").TextContent == "Updated A", timeout: TimeSpan.FromSeconds(2));

        // Assert
        var updatedText = cut.Find(".property-a").TextContent;
        Assert.Equal("Injected A", initialText);
        Assert.Equal("Updated A", updatedText);
    }

    [Fact]
    public void InjectPropertyComponent_SubscribesToModelPropertyB()
    {
        // Arrange
        var model = Services.GetRequiredService<TestInjectedModel>();
        var cut = RenderComponent<TestInjectPropertyComponent>();
        cut.WaitForState(() => cut.Markup.Contains("Injected B"), timeout: TimeSpan.FromSeconds(2));

        // Act
        model.InjectedPropertyB = "Updated B";
        cut.WaitForState(() => cut.Find(".property-b").TextContent == "Updated B", timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal("Updated B", cut.Find(".property-b").TextContent);
    }

    [Fact]
    public void InjectPropertyComponent_SubscribesToModelCounter()
    {
        // Arrange
        var model = Services.GetRequiredService<TestInjectedModel>();
        var cut = RenderComponent<TestInjectPropertyComponent>();
        cut.WaitForState(() => cut.Markup.Contains("0"), timeout: TimeSpan.FromSeconds(2));
        Assert.Equal("0", cut.Find(".counter").TextContent);

        // Act
        model.InjectedCounter = 42;
        cut.WaitForState(() => cut.Find(".counter").TextContent == "42", timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal("42", cut.Find(".counter").TextContent);
    }

    [Fact]
    public void InjectPropertyComponent_SubscribesToInjectedExtraModel()
    {
        // Arrange
        var extraModel = Services.GetRequiredService<TestExtraInjectedModel>();
        var cut = RenderComponent<TestInjectPropertyComponent>();
        cut.WaitForState(() => cut.Markup.Contains("Extra Injected A"), timeout: TimeSpan.FromSeconds(2));
        var initialText = cut.Find(".extra-a").TextContent;

        // Act
        extraModel.InjectedPropertyA = "Updated Extra";
        cut.WaitForState(() => cut.Find(".extra-a").TextContent == "Updated Extra", timeout: TimeSpan.FromSeconds(2));

        // Assert
        var updatedText = cut.Find(".extra-a").TextContent;
        Assert.Equal("Extra Injected A", initialText);
        Assert.Equal("Updated Extra", updatedText);
    }

    [Fact]
    public void InjectDirectiveComponent_SubscribesToModelPropertyA()
    {
        // Arrange
        var model = Services.GetRequiredService<TestInjectedModel>();
        var cut = RenderComponent<TestInjectDirectiveComponent>();
        cut.WaitForState(() => cut.Markup.Contains("Injected A"), timeout: TimeSpan.FromSeconds(2));
        var initialText = cut.Find(".property-a").TextContent;

        // Act
        model.InjectedPropertyA = "Updated from Directive";
        cut.WaitForState(() => cut.Find(".property-a").TextContent == "Updated from Directive", timeout: TimeSpan.FromSeconds(2));

        // Assert
        var updatedText = cut.Find(".property-a").TextContent;
        Assert.Equal("Injected A", initialText);
        Assert.Equal("Updated from Directive", updatedText);
    }

    [Fact]
    public void InjectDirectiveComponent_SubscribesToModelPropertyB()
    {
        // Arrange
        var model = Services.GetRequiredService<TestInjectedModel>();
        var cut = RenderComponent<TestInjectDirectiveComponent>();
        cut.WaitForState(() => cut.Markup.Contains("Injected B"), timeout: TimeSpan.FromSeconds(2));

        // Act
        model.InjectedPropertyB = "Updated from Directive B";
        cut.WaitForState(() => cut.Find(".property-b").TextContent == "Updated from Directive B", timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal("Updated from Directive B", cut.Find(".property-b").TextContent);
    }

    [Fact]
    public void InjectDirectiveComponent_SubscribesToModelCounter()
    {
        // Arrange
        var model = Services.GetRequiredService<TestInjectedModel>();
        var cut = RenderComponent<TestInjectDirectiveComponent>();
        cut.WaitForState(() => cut.Markup.Contains("0"), timeout: TimeSpan.FromSeconds(2));
        Assert.Equal("0", cut.Find(".counter").TextContent);

        // Act
        model.InjectedCounter = 100;
        cut.WaitForState(() => cut.Find(".counter").TextContent == "100", timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal("100", cut.Find(".counter").TextContent);
    }

    [Fact]
    public void InjectDirectiveComponent_SubscribesToInjectedExtraModel()
    {
        // Arrange
        var extraModel = Services.GetRequiredService<TestExtraInjectedModel>();
        var cut = RenderComponent<TestInjectDirectiveComponent>();
        cut.WaitForState(() => cut.Markup.Contains("Extra Injected A"), timeout: TimeSpan.FromSeconds(2));
        var initialText = cut.Find(".extra-a").TextContent;

        // Act
        extraModel.InjectedPropertyA = "Updated Extra Directive";
        cut.WaitForState(() => cut.Find(".extra-a").TextContent == "Updated Extra Directive", timeout: TimeSpan.FromSeconds(2));

        // Assert
        var updatedText = cut.Find(".extra-a").TextContent;
        Assert.Equal("Extra Injected A", initialText);
        Assert.Equal("Updated Extra Directive", updatedText);
    }
}
