using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.AbstractClassInPartialConstructorCodeFixProvider>;
using VerifyCS = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.AbstractClassInPartialConstructorCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG052 - Abstract class cannot be used in partial constructor.
/// </summary>
public class AbstractClassInPartialConstructorTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ConcreteClass_NoErrorsExpected()
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
            public partial class ParentModel : ObservableModel
            {
                // ServiceModel is concrete, not abstract - no error
                public partial ParentModel(ServiceModel serviceModel);

                public partial int Value { get; set; }

                public string ServiceName => ServiceModel.Name;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AbstractObservableModel_DiagnosticExpected()
    {
        // lang=csharp
        // Abstract ObservableModel class - reports RXBG052
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract partial class AbstractBaseModel : ObservableModel
            {
                public abstract void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(AbstractBaseModel baseModel);

                public partial int Value { get; set; }
            }
        }
        """;

        // RXBG052 for abstract ObservableModel class (error) - reported by generator only (SSOT pattern)
        // Also RXBG070 (missing scope on abstract model) and RXBG012 (unused reference)
        var expected = new[]
        {
            DiagnosticResult.CompilerError(DiagnosticDescriptors.AbstractClassInPartialConstructorError.Id)
                .WithSpan(15, 36, 15, 53)
                .WithArguments("baseModel", "Test.AbstractBaseModel"),
            new DiagnosticResult(DiagnosticDescriptors.MissingObservableModelScopeWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithSpan(7, 35, 7, 52)
                .WithArguments("AbstractBaseModel"),
            DiagnosticResult.CompilerError(DiagnosticDescriptors.UnusedModelReferenceError.Id)
                .WithSpan(15, 36, 15, 53)
                .WithArguments("ParentModel", "Test.AbstractBaseModel")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task AbstractNonObservableModel_NoDiagnosticExpected()
    {
        // lang=csharp
        // Abstract non-ObservableModel class (like NavigationManager) - should NOT report RXBG052
        // because the DI container may have concrete implementations registered
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            // Simulates NavigationManager - abstract class but not ObservableModel
            public abstract class NavigationManagerLike
            {
                public abstract string Uri { get; }
                public abstract void NavigateTo(string uri);
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(NavigationManagerLike navigation);

                public partial int Value { get; set; }
            }
        }
        """;

        // Only RXBG050 expected - no RXBG052 since NavigationManagerLike is not an ObservableModel
        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.UnregisteredServiceWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(17, 36, 17, 57)
                .WithArguments("navigation", "NavigationManagerLike", "'services.AddScoped<NavigationManagerLike>()' or 'services.AddScoped<IYourInterface, NavigationManagerLike>()'")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task AbstractObservableModel_CodeFixRemovesParameter()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract partial class AbstractBaseModel : ObservableModel
            {
                public abstract void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(AbstractBaseModel baseModel);

                public partial int Value { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract partial class AbstractBaseModel : ObservableModel
            {
                public abstract void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        // Expected diagnostics: RXBG052 (error), RXBG070 (warning), RXBG012 (error)
        var expectedInitial = new[]
        {
            DiagnosticResult.CompilerError(DiagnosticDescriptors.AbstractClassInPartialConstructorError.Id)
                .WithSpan(15, 36, 15, 53)
                .WithArguments("baseModel", "Test.AbstractBaseModel"),
            new DiagnosticResult(DiagnosticDescriptors.MissingObservableModelScopeWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithSpan(7, 35, 7, 52)
                .WithArguments("AbstractBaseModel"),
            DiagnosticResult.CompilerError(DiagnosticDescriptors.UnusedModelReferenceError.Id)
                .WithSpan(15, 36, 15, 53)
                .WithArguments("ParentModel", "Test.AbstractBaseModel")
        };

        // After fix, the parameter is removed but the abstract model still exists with RXBG070 warning
        var expectedFixed = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.MissingObservableModelScopeWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithSpan(7, 35, 7, 52)
                .WithArguments("AbstractBaseModel")
        };

        await VerifyCS.VerifyCodeFixAsync(test, expectedInitial, fixedCode, expectedFixed, codeActionIndex: 0);
    }

    [Fact]
    public async Task Interface_NoAbstractClassErrorExpected()
    {
        // lang=csharp
        // Interfaces are fine - only RXBG050 info since IService is not registered
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IService
            {
                void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(IService service);

                public partial int Value { get; set; }
            }
        }
        """;

        // Only RXBG050 expected - no RXBG052 since IService is an interface, not abstract class
        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.UnregisteredServiceWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(15, 36, 15, 44)
                .WithArguments("service", "IService", "'services.AddScoped<IService, YourImplementation>()'")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}
