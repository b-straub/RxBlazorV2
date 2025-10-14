using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.DiServiceScopeViolationCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG021 code fix: DI service scope violation code fix
///
/// These tests verify that the code fix correctly:
/// 1. Changes ObservableModelScope attribute to the correct scope when present
/// 2. Adds ObservableModelScope attribute with correct scope when missing
/// 3. Calculates the minimum required scope based on all dependencies
/// </summary>
public class DiServiceScopeViolationCodeFixTests
{
    [Fact]
    public async Task SingletonInjectingScoped_ChangesToScoped()
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
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task SingletonInjectingTransient_ChangesToTransient()
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
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ScopedInjectingTransient_ChangesToTransient()
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
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task SingletonInjectingScoped_NoAttribute_AddsAttribute()
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

            public partial class ConsumerModel : ObservableModel
            {
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task SingletonInjectingTransient_NoAttribute_AddsAttribute()
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

            public partial class ConsumerModel : ObservableModel
            {
                public partial ConsumerModel({|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ServiceModel|} serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task MultipleDependencies_MixedScopes_ChangesToTransient()
    {
        // Test that when multiple dependencies with different scopes are present,
        // the code fix changes to the most permissive scope (Transient in this case)
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
                public partial ConsumerModel(
                    SingletonModel singletonModel,
                    {|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ScopedModel|} scopedModel,
                    {|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:TransientModel|} transientModel);

                public string GetInfo() => SingletonModel.Name + ScopedModel.IsEnabled + TransientModel.Count;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ConsumerModel : ObservableModel
            {
                public partial ConsumerModel(
                    SingletonModel singletonModel,
                    ScopedModel scopedModel,
                    TransientModel transientModel);

                public string GetInfo() => SingletonModel.Name + ScopedModel.IsEnabled + TransientModel.Count;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task SingletonInjectingScopedAndSingleton_ChangesToScoped()
    {
        // Test that when multiple dependencies are present but the most restrictive is Scoped,
        // the code fix changes to Scoped (not Transient)
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

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ConsumerModel : ObservableModel
            {
                public partial ConsumerModel(
                    SingletonModel singletonModel,
                    {|{{DiagnosticDescriptors.DiServiceScopeViolationWarning.Id}}:ScopedModel|} scopedModel);

                public string GetInfo() => SingletonModel.Name + ScopedModel.IsEnabled;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ConsumerModel : ObservableModel
            {
                public partial ConsumerModel(
                    SingletonModel singletonModel,
                    ScopedModel scopedModel);

                public string GetInfo() => SingletonModel.Name + ScopedModel.IsEnabled;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }
}
