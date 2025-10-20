using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class UnusedComponentTriggerDiagnosticTests
{
    [Fact]
    public async Task ModelWithTriggerButNoObservableComponent_ReportsWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ModelWithAsyncTriggerButNoObservableComponent_ReportsWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTriggerAsync|}]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ModelWithBothTriggerTypesButNoObservableComponent_ReportsOneWarningPerProperty()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                [ObservableComponentTriggerAsync]
                public partial string Name { get; set; }
            }
        }
        """;

        // Should only report once per property, not once per attribute
        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ModelWithTriggerAndObservableComponent_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelWithAsyncTriggerAndObservableComponent_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTriggerAsync]
                public partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelWithoutTriggersAndNoObservableComponent_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelWithMultipleTriggersButNoObservableComponent_ReportsMultipleWarnings()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }

                [{|#1:ObservableComponentTriggerAsync|}]
                public partial int Count { get; set; }

                public partial bool IsEnabled { get; set; }
            }
        }
        """;

        var expected1 = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        var expected2 = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(1)
            .WithArguments("Count", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [expected1, expected2]);
    }

    [Fact]
    public async Task ModelWithTriggerAndCustomHookName_ReportsWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger("CustomHookName")|}]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonObservableModelWithTriggers_NoWarning()
    {
        // lang=csharp
        var test = """

        namespace Test
        {
            public partial class RegularClass
            {
                public string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelReferencedByAnotherModel_StillReportsWarning()
    {
        // Note: This is a known limitation - we can't easily detect cross-model references
        // in the analyzer phase. The diagnostic message mentions both conditions.
        // In practice, if a model IS referenced with includeReferencedTriggers: true,
        // the user can ignore this warning.

        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            // SettingsModel has trigger but no [ObservableComponent]
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class SettingsModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial bool IsDarkMode { get; set; }
            }

            // ThemeModel references SettingsModel
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class ThemeModel : ObservableModel
            {
                public partial ThemeModel(SettingsModel settings);
            }
        }
        """;

        // The diagnostic is still reported because we can't easily check references
        // in the analyzer phase. This is documented in the help file.
        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("IsDarkMode", "SettingsModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ModelWithObservableComponentButIncludeReferencedTriggersFalse_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent(includeReferencedTriggers: false)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        // No warning because model has [ObservableComponent]
        // The includeReferencedTriggers parameter doesn't affect triggers on the model itself
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelWithMixedPropertiesAndTriggers_OnlyReportsForTriggeredProperties()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        
        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string NormalProperty { get; set; }

                [{|#0:ObservableComponentTrigger|}]
                public partial string TriggeredProperty { get; set; }

                [ObservableCommand(nameof(ExecuteCommand))]
                public partial IObservableCommand MyCommand { get; }

                private void ExecuteCommand()
                {
                }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("TriggeredProperty", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ModelWithObservableComponentAndCustomName_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent(componentName: "CustomComponent")]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
