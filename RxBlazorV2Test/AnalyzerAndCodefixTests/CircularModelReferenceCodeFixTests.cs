using System.Threading.Tasks;
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
        var test = @"
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
    [ObservableModelReference(typeof(ModelA))]
    public partial class ModelB : ObservableModel
    {
    }
}";

        await AnalyzerVerifier.VerifyAnalyzerAsync(test,
            AnalyzerVerifier.Diagnostic("RXBG006").WithSpan(8, 6, 8, 46).WithArguments("ModelA", "ModelB"),
            AnalyzerVerifier.Diagnostic("RXBG006").WithSpan(14, 6, 14, 46).WithArguments("ModelB", "ModelA"));
    }

    [Fact]
    public async Task RemoveCircularReferenceFromFirstModel_SingleFix()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace TestNamespace
{
    [ObservableModelScope(ModelScope.Singleton)]
    [{|#0:ObservableModelReference(typeof(ModelB))|}]
    public partial class ModelA : ObservableModel
    {
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|#1:ObservableModelReference(typeof(ModelA))|}]
    public partial class ModelB : ObservableModel
    {
    }
}";

        var fixedCode = @"
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
}";

        var expected = new[]
        {
            CodeFixVerifier.Diagnostic("RXBG006").WithLocation(0).WithArguments("ModelA", "ModelB"),
            CodeFixVerifier.Diagnostic("RXBG006").WithLocation(1).WithArguments("ModelB", "ModelA")
        };
        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveCircularReferenceFromBothModels()
    {
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace TestNamespace
{
    [ObservableModelScope(ModelScope.Singleton)]
    [{|#0:ObservableModelReference(typeof(ModelB))|}]
    public partial class ModelA : ObservableModel
    {
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|#1:ObservableModelReference(typeof(ModelA))|}]
    public partial class ModelB : ObservableModel
    {
    }
}";

        var fixedCode = @"
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
}";

        var expected = new[]
        {
            CodeFixVerifier.Diagnostic("RXBG006").WithLocation(0).WithArguments("ModelA", "ModelB"),
            CodeFixVerifier.Diagnostic("RXBG006").WithLocation(1).WithArguments("ModelB", "ModelA")
        };
        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, codeActionIndex: 1);
    }


    [Fact]
    public async Task NoCircularReferenceWithDifferentModels()
    {
        var test = @"
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
}";

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
