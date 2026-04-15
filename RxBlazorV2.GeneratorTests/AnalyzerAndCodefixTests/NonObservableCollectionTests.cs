using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.NonObservableCollectionCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class NonObservableCollectionTests
{
    [Fact]
    public async Task ListProperty_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial {|#0:List<string>|} Names { get; set; } = [];
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonObservableCollectionPropertyError)
            .WithLocation(0)
            .WithArguments("Names", "List<string>", "string");

        await CodeFixVerifier.VerifyAnalyzerAsync(test, [expected]);
    }

    [Fact]
    public async Task IListProperty_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial {|#0:IList<int>|} Items { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonObservableCollectionPropertyError)
            .WithLocation(0)
            .WithArguments("Items", "IList<int>", "int");

        await CodeFixVerifier.VerifyAnalyzerAsync(test, [expected]);
    }

    [Fact]
    public async Task ObservableListProperty_NoDiagnostic()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial ObservableList<string> Names { get; init; } = [];
            }
        }
        """;

        await CodeFixVerifier.VerifyAnalyzerAsync(test, []);
    }

    [Fact]
    public async Task ListProperty_CodeFix_ReplacesWithObservableList()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial {|#0:List<string>|} Names { get; set; } = [];
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial ObservableList<string> Names { get; init; } = [];
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonObservableCollectionPropertyError)
            .WithLocation(0)
            .WithArguments("Names", "List<string>", "string");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task HashSetProperty_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial {|#0:HashSet<int>|} Tags { get; set; } = [];
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonObservableCollectionPropertyError)
            .WithLocation(0)
            .WithArguments("Tags", "HashSet<int>", "int");

        await CodeFixVerifier.VerifyAnalyzerAsync(test, [expected]);
    }

    [Fact]
    public async Task DictionaryProperty_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial {|#0:Dictionary<string, int>|} Counts { get; set; } = [];
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.NonObservableCollectionPropertyError)
            .WithLocation(0)
            .WithArguments("Counts", "Dictionary<string, int>", "KeyValuePair<string, int>");

        await CodeFixVerifier.VerifyAnalyzerAsync(test, [expected]);
    }

    [Fact]
    public async Task NonPartialListProperty_NoDiagnostic()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                // Non-partial properties are not checked
                public List<string> RegularList { get; set; } = [];
            }
        }
        """;

        await CodeFixVerifier.VerifyAnalyzerAsync(test, []);
    }
}
