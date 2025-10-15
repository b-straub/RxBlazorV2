using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class MissingObservableModelScopeDiagnosticTests
{
    [Fact]
    public async Task ObservableModelWithoutScopeAttribute_ReportsWarning()
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

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ObservableModelWithSingletonScope_NoError()
    {
        // lang=csharp
        var test = """

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

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ObservableModelWithScopedScope_NoError()
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
    public async Task ObservableModelWithTransientScope_NoError()
    {
        // lang=csharp
        var test = """

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

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleObservableModelsWithoutScope_ReportsMultipleWarnings()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class {|#0:Model1|} : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class {|#1:Model2|} : ObservableModel
            {
                public partial int Count { get; set; }
            }
        }
        """;

        var expected1 = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("Model1");

        var expected2 = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(1)
            .WithArguments("Model2");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected1, expected2);
    }

    [Fact]
    public async Task NonObservableModelClass_NoError()
    {
        // lang=csharp
        var test = """

        namespace Test
        {
            public partial class RegularClass
            {
                public string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ObservableModelWithOtherAttributes_StillReportsWarning()
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

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.MissingObservableModelScopeWarning)
            .WithLocation(0)
            .WithArguments("TestModel");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }
}
