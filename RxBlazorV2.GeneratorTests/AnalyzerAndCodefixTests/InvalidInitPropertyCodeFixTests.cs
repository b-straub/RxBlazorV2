using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidInitPropertyCodeFixProvider>;

using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class InvalidInitPropertyCodeFixTests
{
    [Fact]
    public async Task ValidRequiredInitForIObservableCollection_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial ObservableList<string> Items { get; init; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvalidInitForNonIObservableCollection_ShowsError()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvalidInitForInt_ShowsError()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial int {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Count|} { get; init; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvalidInitForGenericType_ShowsError()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel<T> : ObservableModel where T : new()
            {
                public required partial T {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Value|} { get; init; } = new();
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidSetAccessorForString_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
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
    public async Task ValidRequiredSetAccessorForString_NoWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InitAccessorWithoutRequired_ShowsError()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; } = "";
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ConvertInvalidInitToSet_PreservesRequired()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string Name { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ConvertInvalidInitToSet_PreservesRequiredAndDefaultValue()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; } = "Default";
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string Name { get; set; } = "Default";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ConvertInvalidInitToSet_MultipleProperties()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; }
                public required partial int {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Age|} { get; init; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial string Name { get; set; }
                public required partial int Age { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ConvertInvalidInitToSet_WithGenericType()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel<T> : ObservableModel where T : new()
            {
                public required partial T {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Value|} { get; init; } = new();
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel<T> : ObservableModel where T : new()
            {
                public required partial T Value { get; set; } = new();
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ValidMixedProperties_OnlyInvalidOnesShowError()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial ObservableList<string> Items { get; init; }
                public required partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; }
                public partial int Count { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ConvertInvalidInitToSet_MixedWithValid()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial ObservableList<string> Items { get; init; }
                public required partial string {|{{DiagnosticDescriptors.InvalidInitPropertyError.Id}}:Name|} { get; init; }
                public partial int Count { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public required partial ObservableList<string> Items { get; init; }
                public required partial string Name { get; set; }
                public partial int Count { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }
}
