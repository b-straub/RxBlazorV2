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
    public async Task AbstractService_DiagnosticExpected()
    {
        // lang=csharp
        // Abstract non-ObservableModel class - reports RXBG052 (twice from analyzer+generator)
        // and RXBG050 (unregistered service info)
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract class AbstractService
            {
                public abstract void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(AbstractService service);

                public partial int Value { get; set; }
            }
        }
        """;

        // RXBG050 for unregistered service (info)
        // RXBG052 for abstract class (error) - reported by generator only (SSOT pattern)
        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.UnregisteredServiceWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(15, 36, 15, 51)
                .WithArguments("service", "AbstractService", "'services.AddScoped<AbstractService>()' or 'services.AddScoped<IYourInterface, AbstractService>()'"),
            DiagnosticResult.CompilerError(DiagnosticDescriptors.AbstractClassInPartialConstructorError.Id)
                .WithSpan(15, 36, 15, 51)
                .WithArguments("service", "Test.AbstractService")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task AbstractService_CodeFixRemovesParameter()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract class AbstractService
            {
                public abstract void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(AbstractService service);

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
            public abstract class AbstractService
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

        // Expected diagnostics: RXBG050 (info), RXBG052 (error, reported by generator only)
        var expectedInitial = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.UnregisteredServiceWarning.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(15, 36, 15, 51)
                .WithArguments("service", "AbstractService", "'services.AddScoped<AbstractService>()' or 'services.AddScoped<IYourInterface, AbstractService>()'"),
            DiagnosticResult.CompilerError(DiagnosticDescriptors.AbstractClassInPartialConstructorError.Id)
                .WithSpan(15, 36, 15, 51)
                .WithArguments("service", "Test.AbstractService")
        };

        // The fix removes the parameter entirely, which removes ALL associated diagnostics
        // Fixed state should have no diagnostics
        await VerifyCS.VerifyCodeFixAsync(test, expectedInitial, fixedCode, expectedFixed: [], codeActionIndex: 0);
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
