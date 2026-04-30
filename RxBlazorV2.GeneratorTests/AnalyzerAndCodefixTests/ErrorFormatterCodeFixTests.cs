using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.MissingErrorFormatterCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for the code fix that scaffolds a stub formatter method when RXBG091 fires
/// (named formatter method does not exist on the model).
/// </summary>
public class ErrorFormatterCodeFixTests
{
    [Fact]
    public async Task MissingFormatter_QuickFixGeneratesStubMethod()
    {
        // lang=csharp
        var source = $$"""
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(LoadData), null, {|{{DiagnosticDescriptors.ErrorFormatterMethodNotFoundError.Id}}:"FormatLoadDataError"|})]
                public partial IObservableCommand LoadDataCommand { get; }

                private void LoadData() { }
            }
        }
        """;

        // The quick fix inserts a private string FormatLoadDataError(Exception ex) stub directly
        // after the command property. The humanized command name is "load data".
        // lang=csharp
        var fixedSource = """
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(LoadData), null, "FormatLoadDataError")]
                public partial IObservableCommand LoadDataCommand { get; }

                private string FormatLoadDataError(System.Exception ex) =>
                $"Failed to load data: {ex.Message}";

                private void LoadData() { }
            }
        }
        """;

        // CS9248 fires in the unfixed state because the analyzer skips codegen for the unresolved
        // formatter; once the stub method is inserted by the fix, codegen runs and CS9248 disappears.
        await CodeFixVerifier.VerifyCodeFixAsync(source, [], fixedSource, codeActionIndex: null, "CS9248");
    }
}
