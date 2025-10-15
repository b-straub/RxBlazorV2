using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.MissingObservableModelScopeCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class MissingObservableModelScopeCodeFixTests
{
    [Fact]
    public async Task AddScopedScopeAttribute_FixesWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class {|#0:TestModel|} : ObservableModel
            {
                public partial string Name { get; set; }
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
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task AddSingletonScopeAttribute_FixesWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class {|#0:TestModel|} : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 1);
    }

    [Fact]
    public async Task AddTransientScopeAttribute_FixesWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class {|#0:TestModel|} : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 2);
    }

    [Fact]
    public async Task AddScopeAttribute_AddsUsingDirective()
    {
        // lang=csharp
        var test = """
        using System;

        namespace Test
        {
            public partial class {|#0:TestModel|} : RxBlazorV2.Model.ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """
        using System;
        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : RxBlazorV2.Model.ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task AddScopeAttribute_PreservesExistingAttributes()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System;

        namespace Test
        {
            [Obsolete]
            public partial class {|#0:TestModel|} : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using System;

        namespace Test
        {
            [Obsolete]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task AddScopeAttribute_PreservesIndentation()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class {|#0:TestModel|} : ObservableModel
            {
                public partial string Name { get; set; }
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
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task AddScopeAttribute_WithComments_PreservesTrivia()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            // This is a test model
            public partial class {|#0:TestModel|} : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            // This is a test model
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }
}
