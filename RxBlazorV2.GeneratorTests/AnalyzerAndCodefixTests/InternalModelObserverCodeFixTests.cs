using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.InternalModelObserverCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG082 (Internal model observer invalid signature warning) code fix functionality.
/// RXBG082 is reported by the generator, and the test infrastructure includes the generator.
/// </summary>
public class InternalModelObserverCodeFixTests
{
    [Fact]
    public async Task PublicMethod_FixToPrivateSync()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                public void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnCounterChanged|}()
                {
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private void OnCounterChanged()
                {
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task PublicMethod_FixToPrivateAsync()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                public void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnCounterChanged|}()
                {
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """
        using RxBlazorV2.Model;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private async Task OnCounterChanged(CancellationToken ct)
                {
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task PrivateMethodWithWrongReturnType_FixToSync()
    {
        // lang=csharp
        // Note: Method body uses simple statements that remain valid after return type change
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private string {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnIsEnabledChanged|}()
                {
                    var _ = Settings.IsEnabled;
                    return "handled";
                }
            }
        }
        """;

        // lang=csharp
        // After fix: return statement will cause CS0127 but the signature fix is correct
        var fixedCode = """
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private void OnIsEnabledChanged()
                {
                    var _ = Settings.IsEnabled;
                    return "handled";
                }
            }
        }
        """;

        // Skip CS0127 compiler error (return statement in void method)
        await CodeFixVerifier.VerifyCodeFixAsync(test, [], fixedCode, codeActionIndex: 0, "CS0127");
    }

    [Fact]
    public async Task PrivateMethodWithWrongReturnType_FixToAsync()
    {
        // lang=csharp
        // Note: Method body uses simple statements that remain valid after signature change
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private string {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnIsEnabledChanged|}()
                {
                    var _ = Settings.IsEnabled;
                    return "handled";
                }
            }
        }
        """;

        // lang=csharp
        // After fix: return statement will cause CS1997 but the signature fix is correct
        var fixedCode = """
        using RxBlazorV2.Model;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private async Task OnIsEnabledChanged()
                {
                    var _ = Settings.IsEnabled;
                    return "handled";
                }
            }
        }
        """;

        // Skip CS1997 compiler error (return value in async Task method)
        await CodeFixVerifier.VerifyCodeFixAsync(test, [], fixedCode, codeActionIndex: 1, "CS1997");
    }

    [Fact]
    public async Task PrivateSyncMethodWithParameter_FixRemovesParameter()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial string Theme { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnThemeChanged|}(string arg)
                {
                    var _ = Settings.Theme;
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial string Theme { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private void OnThemeChanged()
                {
                    var _ = Settings.Theme;
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task PrivateAsyncMethodWithWrongParameter_FixToCancellationToken()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private async Task {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnValueChanged|}(string notAToken)
                {
                    await Task.Delay(100);
                    var _ = Settings.Value;
                }
            }
        }
        """;

        // lang=csharp
        // Note: Using directives are added in the order they're processed (Tasks first, then Threading)
        var fixedCode = """
        using RxBlazorV2.Model;
        using System.Threading.Tasks;
        using System.Threading;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private async Task OnValueChanged(CancellationToken ct)
                {
                    await Task.Delay(100);
                    var _ = Settings.Value;
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task PrivateAsyncMethodWithWrongParameter_FixToNoParameter()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private async Task {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnValueChanged|}(string notAToken)
                {
                    await Task.Delay(100);
                    var _ = Settings.Value;
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """
        using RxBlazorV2.Model;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial int Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                private async Task OnValueChanged()
                {
                    await Task.Delay(100);
                    var _ = Settings.Value;
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }
}
