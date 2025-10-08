using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class SharedModelScopingTests
{
    [Fact]
    public async Task SingleComponentUsingModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

                   using RxBlazorV2.Model;
                   using RxBlazorV2.Interface;
                   using RxBlazorV2.Component;

                   namespace Test
                   {
                       [ObservableModelScope(ModelScope.Scoped)]
                       public partial class TestModel : ObservableModel
                       {
                           public partial string Name { get; set; }
                       }

                       public partial class TestComponent : ObservableComponent<TestModel>
                       {
                       }
                   }
                   """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleComponentsUsingSingletonModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

                   using RxBlazorV2.Model;
                   using RxBlazorV2.Interface;
                   using RxBlazorV2.Component;

                   namespace Test
                   {
                       [ObservableModelScope(ModelScope.Singleton)]
                       public partial class TestModel : ObservableModel
                       {
                           public partial string Name { get; set; }
                       }

                       public partial class TestComponent1 : ObservableComponent<TestModel>
                       {
                       }

                       public partial class TestComponent2 : ObservableComponent<TestModel>
                       {
                       }
                   }
                   """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleComponentsUsingDefaultScopeModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

                   using RxBlazorV2.Model;
                   using RxBlazorV2.Interface;
                   using RxBlazorV2.Component;

                   namespace Test
                   {
                       // No scope attribute - defaults to Singleton
                       public partial class TestModel : ObservableModel
                       {
                           public partial string Name { get; set; }
                       }

                       public partial class TestComponent1 : ObservableComponent<TestModel>
                       {
                       }

                       public partial class TestComponent2 : ObservableComponent<TestModel>
                       {
                       }
                   }
                   """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleComponentsUsingScopedModel_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

                     using RxBlazorV2.Model;
                     using RxBlazorV2.Interface;
                     using RxBlazorV2.Component;

                     namespace Test
                     {
                         [{|{{DiagnosticDescriptors.SharedModelNotSingletonError.Id}}:ObservableModelScope(ModelScope.Scoped)|}]
                         public partial class TestModel : ObservableModel
                         {
                             public partial string Name { get; set; }
                         }

                         public partial class TestComponent1 : ObservableComponent<TestModel>
                         {
                         }

                         public partial class TestComponent2 : ObservableComponent<TestModel>
                         {
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleComponentsUsingTransientModel_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

                     using RxBlazorV2.Model;
                     using RxBlazorV2.Interface;
                     using RxBlazorV2.Component;

                     namespace Test
                     {
                         [{|{{DiagnosticDescriptors.SharedModelNotSingletonError.Id}}:ObservableModelScope(ModelScope.Transient)|}]
                         public partial class TestModel : ObservableModel
                         {
                             public partial string Name { get; set; }
                         }

                         public partial class TestComponent1 : ObservableComponent<TestModel>
                         {
                         }

                         public partial class TestComponent2 : ObservableComponent<TestModel>
                         {
                         }
                     }
                     """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }
}