using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.NonPublicPartialConstructorCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class NonPublicPartialConstructorCodeFixTests
{
    [Fact]
    public async Task ProtectedPartialConstructor_ChangesToPublic()
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

        // lang=csharp
        var fixedCode = """

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

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "protected");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }

    [Fact]
    public async Task PrivatePartialConstructor_ChangesToPublic()
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

        // lang=csharp
        var fixedCode = """

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

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "private");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }

    [Fact]
    public async Task InternalPartialConstructor_ChangesToPublic()
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

        // lang=csharp
        var fixedCode = """

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

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "internal");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }

    [Fact]
    public async Task ProtectedInternalPartialConstructor_ChangesToPublic()
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

        // lang=csharp
        var fixedCode = """

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

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "protected internal");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }

    [Fact]
    public async Task ProtectedPartialConstructor_WithMultipleParameters_ChangesToPublic()
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

        // lang=csharp
        var fixedCode = """

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

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "protected");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }

    [Fact]
    public async Task ProtectedPartialConstructor_PreservesComments()
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

                // Constructor with DI
                protected partial {|#0:TestModel|}(HttpClient dependency);
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }

                // Constructor with DI
                public partial TestModel(HttpClient dependency);
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonPublicPartialConstructorError)
            .WithLocation(0)
            .WithArguments("TestModel", "protected");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }
}
