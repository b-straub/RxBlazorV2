using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG091 (Error formatter method not found) and RXBG092 (Error formatter
/// method has invalid signature). Both are analyzer-reported per the SSOT pattern;
/// generator skips code generation for the affected command property.
/// </summary>
public class ErrorFormatterDiagnosticTests
{
    [Fact]
    public async Task NoFormatterArg_NoDiagnostic()
    {
        // lang=csharp
        var test = """
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run))]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidInstanceFormatter_NoDiagnostic()
    {
        // lang=csharp
        var test = """
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, nameof(FormatRunError))]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }

                private string FormatRunError(System.Exception ex) =>
                    $"Failed to run: {ex.Message}";
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidStaticFormatter_NoDiagnostic()
    {
        // lang=csharp
        var test = """
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, nameof(FormatRunError))]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }

                private static string FormatRunError(System.Exception ex) =>
                    $"Failed to run: {ex.Message}";
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task FormatterMissing_RaisesRXBG091()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, {|{{DiagnosticDescriptors.ErrorFormatterMethodNotFoundError.Id}}:"FormatRunError"|})]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }
            }
        }
        """;

        // CS9248 fires because the analyzer skips codegen for the unresolved formatter,
        // leaving the partial command property without an implementation part.
        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [], "CS9248");
    }

    [Fact]
    public async Task FormatterWrongReturnType_RaisesRXBG092()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, {|{{DiagnosticDescriptors.ErrorFormatterMethodInvalidSignatureError.Id}}:nameof(FormatRunError)|})]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }

                // Wrong: returns void
                private void FormatRunError(System.Exception ex) { }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [], "CS9248");
    }

    [Fact]
    public async Task FormatterWrongParamType_RaisesRXBG092()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, {|{{DiagnosticDescriptors.ErrorFormatterMethodInvalidSignatureError.Id}}:nameof(FormatRunError)|})]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }

                // Wrong: parameter is not System.Exception
                private string FormatRunError(string message) => message;
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [], "CS9248");
    }

    [Fact]
    public async Task FormatterTooManyParams_RaisesRXBG092()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, {|{{DiagnosticDescriptors.ErrorFormatterMethodInvalidSignatureError.Id}}:nameof(FormatRunError)|})]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }

                // Wrong: extra parameter
                private string FormatRunError(System.Exception ex, int extra) => ex.Message;
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [], "CS9248");
    }

    [Fact]
    public async Task FormatterParamIsDerivedException_RaisesRXBG092()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Run), null, {|{{DiagnosticDescriptors.ErrorFormatterMethodInvalidSignatureError.Id}}:nameof(FormatRunError)|})]
                public partial IObservableCommand RunCommand { get; }

                private void Run() { }

                // Wrong: parameter is a derived exception, not System.Exception itself
                private string FormatRunError(System.InvalidOperationException ex) => ex.Message;
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [], "CS9248");
    }
}
