using RxBlazorV2Generator.Diagnostics;

using CodeFixVerifier =
    RxBlazorV2Test.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.GenericConstraintCodeFixProvider>;

using AnalyzerVerifier =
    RxBlazorV2Test.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2Test.Tests;

public class GenericConstraintCodeFixTests
{
    [Fact]
    public async Task AdjustTypeParameters_ToMatchReferencedModel()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModelTwoParams<T, U> : ObservableModel
    {
        public partial T Value { get; set; }
        public partial U SecondValue { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|" + DiagnosticDescriptors.GenericArityMismatchError.Id + @":ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModelTwoParams<T, U> : ObservableModel
    {
        public partial T Value { get; set; }
        public partial U SecondValue { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [ObservableModelReference(typeof(GenericModelTwoParams<,>))]
    public partial class TestModel<T, U> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveGenericArityMismatchReference()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModelTwoParams<T, U> : ObservableModel
    {
        public partial T Value { get; set; }
        public partial U SecondValue { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|" + DiagnosticDescriptors.GenericArityMismatchError.Id + @":ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModelTwoParams<T, U> : ObservableModel
    {
        public partial T Value { get; set; }
        public partial U SecondValue { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task AdjustTypeParameters_ForInvalidOpenGenericReference()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|" + DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id + @":ObservableModelReference(typeof(GenericModel<>))|}]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [ObservableModelReference(typeof(GenericModel<>))]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveTypeConstraintMismatchReference()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel where T : class
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|" + DiagnosticDescriptors.TypeConstraintMismatchError.Id + @":ObservableModelReference(typeof(GenericModel<>))|}]
    public partial class TestModel<T> : ObservableModel where T : struct
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel where T : class
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel<T> : ObservableModel where T : struct
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveInvalidOpenGenericReference()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|" + DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id + @":ObservableModelReference(typeof(GenericModel<>))|}]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task RemoveGenericArityMismatchReference_FromAttributeList()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModelTwoParams<T, U> : ObservableModel
    {
        public partial T Value { get; set; }
        public partial U SecondValue { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [SuppressMessage(""Test"", ""TST001""), {|" + DiagnosticDescriptors.GenericArityMismatchError.Id + @":ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModelTwoParams<T, U> : ObservableModel
    {
        public partial T Value { get; set; }
        public partial U SecondValue { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [SuppressMessage(""Test"", ""TST001"")]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task RemoveTypeConstraintMismatchReference_SeparateAttributeList()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    public interface IEntity { }

    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel where T : class, IEntity, new()
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [{|" + DiagnosticDescriptors.TypeConstraintMismatchError.Id + @":ObservableModelReference(typeof(GenericModel<>))|}]
    public partial class TestModel<T> : ObservableModel where T : class, new()
    {
        public partial string Name { get; set; } = """";
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    public interface IEntity { }

    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel where T : class, IEntity, new()
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel<T> : ObservableModel where T : class, new()
    {
        public partial string Name { get; set; } = """";
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }
}
