using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.GenericConstraintCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class GenericConstraintCodeFixTests
{
    [Fact]
    public async Task AdjustTypeParameters_ToMatchReferencedModel()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.GenericArityMismatchError.Id}}:ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
            public partial class TestModel<T> : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

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
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
            public partial class TestModel<T, U> : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveGenericArityMismatchReferenceAsync()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.GenericArityMismatchError.Id}}:ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
            public partial class TestModel<T> : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task AdjustTypeParameters_ForInvalidOpenGenericReference()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

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
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveTypeConstraintMismatchReferenceAsync()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : struct
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task RemoveInvalidOpenGenericReferenceAsync()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task RemoveGenericArityMismatchReference_FromAttributeList()
    {
        // lang=csharp
        var test = $$"""

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
            [SuppressMessage("Test", "TST001"), {|{{DiagnosticDescriptors.GenericArityMismatchError.Id}}:ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
            public partial class TestModel<T> : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
            [SuppressMessage("Test", "TST001")]
            public partial class TestModel<T> : ObservableModel
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task RemoveTypeConstraintMismatchReference_SeparateAttributeList()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : class, new()
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task AdjustTypeConstraints_AddMissingConstraints()
    {
        // lang=csharp
        var test = $$"""

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
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : class
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

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
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : class, IEntity, new()
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task AdjustMultipleTypeParameters_WithDifferentConstraints()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IKey { }
            public interface IValue { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class DictionaryModel<TKey, TValue> : ObservableModel
                where TKey : class, IKey, new()
                where TValue : struct, IValue
            {
                public partial TKey Key { get; set; }
                public partial TValue Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(DictionaryModel<,>))|}]
            public partial class TestModel<TKey, TValue> : ObservableModel
                where TKey : class, new()
                where TValue : struct
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IKey { }
            public interface IValue { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class DictionaryModel<TKey, TValue> : ObservableModel
                where TKey : class, IKey, new()
                where TValue : struct, IValue
            {
                public partial TKey Key { get; set; }
                public partial TValue Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference(typeof(DictionaryModel<,>))|}]
            public partial class TestModel<TKey, TValue> : ObservableModel
                where TKey : class, IKey, new()
                where TValue : struct, IValue
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task AdjustTypeParameters_WithUnmanagedConstraint()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T> : ObservableModel where T : unmanaged
            {
                public partial T Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : struct
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T> : ObservableModel where T : unmanaged
            {
                public partial T Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : unmanaged
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task AdjustTypeParameters_WithBaseTypeConstraint()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class BaseEntity
            {
                public int Id { get; set; }
            }

            public interface IEntity { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T> : ObservableModel where T : BaseEntity, IEntity, new()
            {
                public partial T Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : new()
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class BaseEntity
            {
                public int Id { get; set; }
            }

            public interface IEntity { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T> : ObservableModel where T : BaseEntity, IEntity, new()
            {
                public partial T Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class TestModel<T> : ObservableModel where T : BaseEntity, IEntity, new()
            {
                public partial string Name { get; set; } = "";
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }
}
