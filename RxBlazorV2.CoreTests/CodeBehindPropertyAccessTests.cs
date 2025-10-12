using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;
using Xunit;

namespace RxBlazorV2.CoreTests;

public class CodeBehindPropertyAccessTests : Bunit.TestContext
{

    [Fact]
    public void Component_DetectsPropertyAccessViaExpressionBodiedProperty()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        // Change PropertyB which is accessed via expression-bodied property
        cut.Instance.Model.PropertyB = "Test Value B";

        // Assert - Component should re-render when PropertyB changes
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("Test Value B", markup);
        });
    }

    [Fact]
    public void Component_DetectsPropertyAccessViaRegularPropertyGetter()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        // Change PropertyC which is accessed via regular property with getter
        cut.Instance.Model.PropertyC = "Test Value C";

        // Assert - Component should re-render when PropertyC changes
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("Test Value C", markup);
        });
    }

    [Fact]
    public void Component_DetectsPropertyAccessViaMethod()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        // Change Counter which is accessed via method
        cut.Instance.Model.Counter = 42;

        // Assert - Component should re-render when Counter changes
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("42", markup);
        });
    }

    [Fact]
    public void Component_DetectsDirectPropertyAccessFromRazor()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        // Change PropertyA which is directly accessed in razor
        cut.Instance.Model.PropertyA = "Direct Value A";

        // Assert - Component should re-render when PropertyA changes
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("Direct Value A", markup);
        });
    }

    [Fact]
    public void Component_DetectsPropertyAccessInConditionalLogic()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        var renderCount = 0;
        cut.Instance.Model.Observable.Subscribe(_ => renderCount++);

        // Change IsActive which is accessed in conditional logic
        cut.Instance.Model.IsActive = true;

        // Assert - Component should observe changes to IsActive
        cut.WaitForAssertion(() =>
        {
            Assert.True(renderCount > 0);
        });
    }

    [Fact]
    public void Component_SubscribesToAllCodeBehindAccessedProperties()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        var notifiedProperties = new HashSet<string>();
        cut.Instance.Model.Observable.Subscribe(props =>
        {
            foreach (var prop in props)
            {
                notifiedProperties.Add(prop);
            }
        });

        // Act - Change all properties
        cut.Instance.Model.PropertyA = "A";
        cut.Instance.Model.PropertyB = "B";
        cut.Instance.Model.PropertyC = "C";
        cut.Instance.Model.Counter = 1;
        cut.Instance.Model.IsActive = true;

        // Assert - All properties should trigger notifications
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("PropertyA", notifiedProperties);
            Assert.Contains("PropertyB", notifiedProperties);
            Assert.Contains("PropertyC", notifiedProperties);
            Assert.Contains("Counter", notifiedProperties);
            Assert.Contains("IsActive", notifiedProperties);
        });
    }

    [Fact]
    public void Component_RenderUpdates_WhenCodeBehindAccessedPropertiesChange()
    {
        // Arrange
        Services.AddTransient<TestCodeBehindAccessModel>();

        // Act
        var cut = RenderComponent<TestCodeBehindAccessComponent>();

        // Wait for initialization
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Model));

        var initialMarkup = cut.Markup;

        // Change properties accessed in code-behind
        cut.Instance.Model.PropertyB = "Updated B";
        cut.Instance.Model.PropertyC = "Updated C";
        cut.Instance.Model.Counter = 99;

        // Assert - Markup should update
        cut.WaitForAssertion(() =>
        {
            var updatedMarkup = cut.Markup;
            Assert.NotEqual(initialMarkup, updatedMarkup);
            Assert.Contains("Updated B", updatedMarkup);
            Assert.Contains("Updated C", updatedMarkup);
            Assert.Contains("99", updatedMarkup);
        });
    }
}
