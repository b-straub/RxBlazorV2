using System.Threading.Tasks;
using RxBlazorV2Generator.Diagnostics;
using Xunit;

using CodeFixVerifier =
    RxBlazorV2Test.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.CircularModelReferenceCodeFixProvider>;

using AnalyzerVerifier =
    RxBlazorV2Test.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2Test.AnalyzerAndCodefixTests;

public class CircularModelReferenceCodeFixTests
{
    [Fact]
    public async Task DetectSimpleCircularReference()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB))|}]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelB : ObservableModel
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RemoveCircularReferenceFromFirstModel_SingleFix()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB))|}]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelB : ObservableModel
            {
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
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference(typeof(ModelA))]
            public partial class ModelB : ObservableModel
            {
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveCircularReferenceFromBothModels()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB))|}]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelB : ObservableModel
            {
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
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ModelB : ObservableModel
            {
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }


    [Fact]
    public async Task NoCircularReferenceWithDifferentModels()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference(typeof(ModelB))]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference(typeof(ModelC))]
            public partial class ModelB : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ModelC : ObservableModel
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectMultipleCircularReferences_InSameCodebase()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB))|}]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelB : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelD))|}]
            public partial class ModelC : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelC))|}]
            public partial class ModelD : ObservableModel
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectCircularReference_WithGenericModel()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class ModelA<T> : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA<>))|}]
            public partial class GenericModel<T> : ObservableModel
            {
                public partial T Value { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectSelfReferencingModel()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelA : ObservableModel
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RemoveSelfReference()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace TestNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelA : ObservableModel
            {
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
            public partial class ModelA : ObservableModel
            {
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }
}
