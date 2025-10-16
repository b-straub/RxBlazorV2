using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG021: DI service scope violation
///
/// Note: These tests rely on ServiceAnalyzer detecting service registrations in the compilation.
/// In test scenarios without explicit service registrations, the scope detection may return null.
/// The tests with ObservableModel dependencies will work since ObservableModels have scope attributes.
/// </summary>
public class DiScopeViolationDiagnosticTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task SingletonInjectingSingleton_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ConsumerModel : ObservableModel
            {
                // Singleton injecting Singleton - OK
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ScopedInjectingSingleton_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ConsumerModel : ObservableModel
            {
                // Scoped injecting Singleton - OK
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ScopedInjectingScoped_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ConsumerModel : ObservableModel
            {
                // Scoped injecting Scoped - OK
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TransientInjectingSingleton_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ConsumerModel : ObservableModel
            {
                // Transient injecting Singleton - OK
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TransientInjectingScoped_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ConsumerModel : ObservableModel
            {
                // Transient injecting Scoped - OK
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TransientInjectingTransient_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ConsumerModel : ObservableModel
            {
                // Transient injecting Transient - OK
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task SingletonInjectingScoped_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ConsumerModel : ObservableModel
            {
                // Singleton injecting Scoped - VIOLATION (captive dependency)
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationError.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task SingletonInjectingTransient_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ConsumerModel : ObservableModel
            {
                // Singleton injecting Transient - VIOLATION (captive dependency)
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationError.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ScopedInjectingTransient_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ConsumerModel : ObservableModel
            {
                // Scoped injecting Transient - VIOLATION (disposal issues)
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationError.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleDependencies_MixedScopes_DiagnosticsOnViolationsOnly()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class SingletonModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ScopedModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class TransientModel : ObservableModel
            {
                public partial int Count { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ConsumerModel : ObservableModel
            {
                // Mixed: SingletonModel OK, ScopedModel and TransientModel are violations
                public partial ConsumerModel(
                    SingletonModel singletonModel,
                    {|{{DiagnosticDescriptors.DiServiceScopeViolationError.Id}}:ScopedModel|} scopedModel,
                    {|{{DiagnosticDescriptors.DiServiceScopeViolationError.Id}}:TransientModel|} transientModel);

                // Use properties to avoid DiServiceScopeViolationError
                public string GetInfo() => SingletonModel.Name + ScopedModel.IsEnabled + TransientModel.Count;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ScopedModel_MultipleDependencies_OnlyTransientViolation()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class SingletonModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ScopedModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class TransientModel : ObservableModel
            {
                public partial int Count { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ConsumerModel : ObservableModel
            {
                // Mixed: SingletonModel and ScopedModel OK, TransientModel is violation
                public partial ConsumerModel(
                    SingletonModel singletonModel,
                    ScopedModel scopedModel,
                    {|{{DiagnosticDescriptors.DiServiceScopeViolationError.Id}}:TransientModel|} transientModel);

                // Use properties to avoid DiServiceScopeViolationError
                public string GetInfo() => SingletonModel.Name + ScopedModel.IsEnabled + TransientModel.Count;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
