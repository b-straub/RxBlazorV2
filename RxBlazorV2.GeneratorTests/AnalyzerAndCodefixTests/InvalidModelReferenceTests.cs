using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class InvalidModelReferenceTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidModelReference_NoErrorsExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
           public partial class ValidModel : ObservableModel
           {
               public partial string Name { get; set; }
           }

           [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference<ValidModel>|}]
           [ObservableModelScope(ModelScope.Scoped)]
           public partial class TestClass : ObservableModel
           {
               public partial int Value { get; set; }
           }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvalidModelReference_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class InvalidModel
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<InvalidModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestClass : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RemoveInvalidAttributeCodeFix_RemovesAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class InvalidModel
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<InvalidModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestClass : ObservableModel
            {
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
            public class InvalidModel
            {
                public string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestClass : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task MakeClassObservableCodeFix_AddsInheritance()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class InvalidModel
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<InvalidModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestClass : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class InvalidModel : ObservableModel
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference<InvalidModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestClass : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }
}