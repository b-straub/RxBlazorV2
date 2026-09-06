using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.UnusedComponentBatchCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Code fixes for RXBG044 - either give the model a component so the generated hook exists,
/// or remove the attribute.
/// </summary>
public class UnusedComponentBatchCodeFixTests
{
    [Fact]
    public async Task AddObservableComponentAttribute_FixesWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentBatchAsync("search", 250)|}]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentBatchAsync("search", 250)]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedComponentBatchWarning)
            .WithLocation(0)
            .WithArguments("search", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task RemoveBatchAttributes_FixesWarning()
    {
        // Every member of the batch loses the attribute, not just the one carrying the diagnostic:
        // a batch only means anything as a group.
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentBatchAsync("search", 250)|}]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string SearchTerm { get; set; }

                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedComponentBatchWarning)
            .WithLocation(0)
            .WithArguments("search", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 1);
    }
}
