using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Diagnostics for [ObservableComponentBatchAsync]: RXBG043 (unusable declaration) and
/// RXBG044 (no component to host the hook).
/// </summary>
public class ComponentBatchDiagnosticTests
{
    [Fact]
    public async Task MixedDebounceWindowsInOneBatch_ReportsNothing()
    {
        // The per-property window is the point of the attribute: the typed term settles while the
        // switch fires at once. Differing windows inside a batch are a feature, not a conflict.
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentBatchAsync("search", 250)]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task SeparateBatches_ReportNothing()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentBatchAsync("search", 250)]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("paging", 500)]
                public partial int PageSize { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ObservableBatch_ReportsNothing()
    {
        // Regression guard for the revert: [ObservableBatch] is a plain SuspendNotifications group,
        // so its id has no identifier constraint and it never needs [ObservableComponent].
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableBatch("search results")]
                public partial string SearchTerm { get; set; }

                [ObservableBatch("search results")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task BatchIdIsNotAnIdentifier_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentBatchAsync("search results", 250)|}]
                public partial string SearchTerm { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.InvalidComponentBatchError)
            .WithLocation(0)
            .WithArguments("search results", "the batch id is not a valid C# identifier, so no hook method name can be formed");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task BatchIdIsKeyword_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentBatchAsync("class", 250)|}]
                public partial string SearchTerm { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.InvalidComponentBatchError)
            .WithLocation(0)
            .WithArguments("class", "the batch id is not a valid C# identifier, so no hook method name can be formed");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NegativeDebounceWindow_ReportsError()
    {
        // Not clamped to zero: silently reinterpreting a negative window would hide a typo that
        // changes behaviour.
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentBatchAsync("search", -250)|}]
                public partial string SearchTerm { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.InvalidComponentBatchError)
            .WithLocation(0)
            .WithArguments("search", "the debounce window is negative (-250 ms)");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task BatchWithoutObservableComponent_ReportsWarning()
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

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.UnusedComponentBatchWarning)
            .WithLocation(0)
            .WithArguments("search", "TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }
}
