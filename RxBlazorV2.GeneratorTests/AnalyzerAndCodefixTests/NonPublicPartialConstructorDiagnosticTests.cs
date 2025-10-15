using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class NonPublicPartialConstructorDiagnosticTests
{
    [Fact]
    public async Task ProtectedPartialConstructorWithParameters_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                protected partial {|#0:TestModel|}(HttpClient dependency);
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "protected");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PrivatePartialConstructorWithParameters_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                private partial {|#0:TestModel|}(HttpClient dependency);
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "private");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task InternalPartialConstructorWithParameters_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                internal partial {|#0:TestModel|}(HttpClient dependency);
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "internal");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PublicPartialConstructorWithParameters_NoError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                public partial TestModel(HttpClient dependency);
            }
        }
        """;

        // No RXBG071 error for public constructor
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ProtectedPartialConstructorWithoutParameters_NoError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                protected TestModel() { }
            }
        }
        """;

        // Parameterless constructors can be any visibility - no error expected
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NoPartialConstructor_NoError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ProtectedInternalPartialConstructor_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                protected internal partial {|#0:TestModel|}(HttpClient dependency);
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "protected internal");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }
}
