using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class GenericModelDiagnosticTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidGenericModelReference_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

                   using RxBlazorV2.Model;
                   using RxBlazorV2.Interface;

                   namespace Test
                   {
                       [ObservableModelScope(ModelScope.Singleton)]
                       public partial class GenericModel<T> : ObservableModel where T : class
                       {
                           public partial T Item { get; set; }
                       }

                       [ObservableModelReference(typeof(GenericModel<>))]
                       [ObservableModelScope(ModelScope.Scoped)]
                       public partial class ConsumerModel<T> : ObservableModel where T : class
                       {
                           public partial int Value { get; set; }
                           
                           public T GetProp()
                           {
                               return GenericModel.Item;
                           }
                       }
                   }
                   """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task GenericArityMismatch_SingleToDouble_DiagnosticExpected()
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
                             public partial T Item { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.GenericArityMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel<T, P> : ObservableModel where T : class where P : struct
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task GenericArityMismatch_DoubleToSingle_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

                     using RxBlazorV2.Model;
                     using RxBlazorV2.Interface;

                     namespace Test
                     {
                         [ObservableModelScope(ModelScope.Singleton)]
                         public partial class GenericModel<T, P> : ObservableModel where T : class where P : struct
                         {
                             public partial T Item1 { get; set; }
                             public partial P Item2 { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.GenericArityMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<,>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel<T> : ObservableModel where T : class
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TypeConstraintMismatch_ClassToStruct_DiagnosticExpected()
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
                             public partial T Item { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel<T> : ObservableModel where T : struct
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TypeConstraintMismatch_SpecificInterfaceConstraint_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

                     using RxBlazorV2.Model;
                     using RxBlazorV2.Interface;
                     using System;

                     namespace Test
                     {
                         [ObservableModelScope(ModelScope.Singleton)]
                         public partial class GenericModel<T> : ObservableModel where T : IDisposable
                         {
                             public partial T Item { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel<T> : ObservableModel where T : class
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvalidOpenGenericReference_FromNonGenericClass_DiagnosticExpected()
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
                             public partial T Item { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel : ObservableModel
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvalidOpenGenericReference_MultipleTypeParameters_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

                     using RxBlazorV2.Model;
                     using RxBlazorV2.Interface;

                     namespace Test
                     {
                         [ObservableModelScope(ModelScope.Singleton)]
                         public partial class GenericModel<T, P> : ObservableModel where T : class where P : struct
                         {
                             public partial T Item1 { get; set; }
                             public partial P Item2 { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id}}:ObservableModelReference(typeof(GenericModel<,>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel : ObservableModel
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TypeConstraintMismatch_MoreRestrictive_DiagnosticExpected()
    {
        // lang=csharp
        // Note: More restrictive constraints are currently not supported
        var test = $$"""

                     using RxBlazorV2.Model;
                     using RxBlazorV2.Interface;
                     using System;

                     namespace Test
                     {
                         [ObservableModelScope(ModelScope.Singleton)]
                         public partial class GenericModel<T> : ObservableModel where T : class
                         {
                             public partial T Item { get; set; }
                         }

                         [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
                         [ObservableModelScope(ModelScope.Scoped)]
                         public partial class ConsumerModel<T> : ObservableModel where T : class, IDisposable
                         {
                             public partial int Value { get; set; }
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidGenericModelReference_SameConstraints_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

                   using RxBlazorV2.Model;
                   using RxBlazorV2.Interface;
                   using System;

                   namespace Test
                   {
                       [ObservableModelScope(ModelScope.Singleton)]
                       public partial class GenericModel<T> : ObservableModel where T : class
                       {
                           public partial T Item { get; set; }
                       }

                       [ObservableModelReference(typeof(GenericModel<>))]
                       [ObservableModelScope(ModelScope.Scoped)]
                       public partial class ConsumerModel<T> : ObservableModel where T : class
                       {
                           public partial int Value { get; set; }
                           
                           public T GetProp()
                           {
                               return GenericModel.Item;
                           }
                       }
                   }
                   """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }
}
