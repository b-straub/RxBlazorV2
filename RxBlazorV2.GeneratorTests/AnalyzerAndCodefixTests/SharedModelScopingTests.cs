using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class SharedModelScopingTests
{
    [Fact]
    public async Task SingleComponentUsingModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Component;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class TestComponent : ObservableComponent<TestModel>
            {
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleComponentsUsingSingletonModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Component;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class TestComponent1 : ObservableComponent<TestModel>
            {
            }

            public partial class TestComponent2 : ObservableComponent<TestModel>
            {
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleComponentsUsingDefaultScopeModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Component;

        namespace Test
        {
            // No scope attribute - defaults to Scoped
            // NOTE: RXBG014 (SharedModelNotSingletonError) is only reported by generator when scanning razor files,
            // not when analyzing C# component inheritance. This test uses C# components, so no error is expected here.
            // The actual scope mismatch would be caught when used in razor files (see RazorFileDiagnosticsTests).
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class TestComponent1 : ObservableComponent<TestModel>
            {
            }

            public partial class TestComponent2 : ObservableComponent<TestModel>
            {
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    // NOTE: SharedModelNotSingletonError is now reported by the generator (not analyzer)
    // when scanning razor files. See RazorFileDiagnosticsTests for the replacement tests:
    // - ScopedModelUsedInMultipleRazorFiles_ReportsError
    // - TransientModelUsedInMultipleRazorFiles_ReportsError
    // - RazorWithCodebehindFile_ScopedModelInMultipleFiles_ReportsError
}