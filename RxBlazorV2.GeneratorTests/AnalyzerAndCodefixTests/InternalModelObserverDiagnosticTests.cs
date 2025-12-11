using RxBlazorV2Generator.Diagnostics;
using GeneratorVerifier = RxBlazorV2.GeneratorTests.Helpers.RxBlazorGeneratorVerifier;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG082 (Internal model observer invalid signature warning).
/// RXBG082 is reported by the generator because it requires cross-model analysis
/// to detect when a method accesses properties from a referenced ObservableModel.
/// </summary>
public class InternalModelObserverDiagnosticTests
{
    [Fact]
    public async Task PublicMethodWithObserverName_AccessingReferencedModelProperty_ReportsWarning()
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

                // Public method with observer naming pattern - should generate RXBG082
                public void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnCounterChanged|}()
                {
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task PrivateMethodWithWrongReturnType_AccessingReferencedModelProperty_ReportsWarning()
    {
        // lang=csharp
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

                // Private method with wrong return type
                private bool {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnIsEnabledChanged|}()
                {
                    return Settings.IsEnabled;
                }
            }
        }
        """;

        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task PrivateMethodWithParameters_AccessingReferencedModelProperty_ReportsWarning()
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

                // Private sync method with parameters (invalid for sync observer)
                private void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnThemeChanged|}(string arg)
                {
                    System.Console.WriteLine($"Theme: {Settings.Theme}, Arg: {arg}");
                }
            }
        }
        """;

        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task PrivateAsyncMethodWithWrongParameter_AccessingReferencedModelProperty_ReportsWarning()
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

                // Async method with wrong parameter type (not CancellationToken)
                private async Task {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:OnValueChanged|}(string notACancellationToken)
                {
                    await Task.Delay(100);
                    var _ = Settings.Value;
                }
            }
        }
        """;

        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task MethodWithNonObserverName_AccessingReferencedModelProperty_DoesNotReportWarning()
    {
        // lang=csharp
        var test = """
        using RxBlazorV2.Model;

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

                // Method with non-observer name - should NOT generate RXBG082
                public void ProcessValue()
                {
                    var _ = Settings.Value;
                }
            }
        }
        """;

        // No diagnostics expected
        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task ValidPrivateSyncObserver_DoesNotReportWarning()
    {
        // lang=csharp
        var test = """
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

                // Valid private sync observer - no diagnostic
                private void OnCounterChanged()
                {
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        // No diagnostics expected
        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task ValidPrivateAsyncObserverWithCancellationToken_DoesNotReportWarning()
    {
        // lang=csharp
        var test = """
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

                // Valid private async observer with CancellationToken - no diagnostic
                private async Task OnCounterChangedAsync(CancellationToken ct)
                {
                    await Task.Delay(100, ct);
                    var _ = Settings.Counter;
                }
            }
        }
        """;

        // No diagnostics expected
        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task HandleMethodPrefix_ReportsWarningForInvalidSignature()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                // Handle* prefix triggers RXBG082
                public void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:HandleNameUpdate|}()
                {
                    var _ = Settings.Name;
                }
            }
        }
        """;

        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }

    [Fact]
    public async Task ObserverSuffix_ReportsWarningForInvalidSignature()
    {
        // lang=csharp
        var test = $$"""
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsActive { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(SettingsModel settings);

                // *Observer suffix triggers RXBG082
                public void {|{{DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id}}:IsActiveObserver|}()
                {
                    var _ = Settings.IsActive;
                }
            }
        }
        """;

        await GeneratorVerifier.VerifyGeneratorDiagnosticsAsync(test);
    }
}
