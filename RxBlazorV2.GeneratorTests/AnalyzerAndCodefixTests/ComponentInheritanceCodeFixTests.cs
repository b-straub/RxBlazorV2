using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.ComponentInheritanceCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class ComponentInheritanceCodeFixTests
{
    [Fact]
    public async Task ChangeToObservableComponent_FixesComponentBaseInheritance()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.AspNetCore.Components;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.ComponentNotObservableError.Id}}:ObservableModelScope(ModelScope.Singleton)|}]
            public partial class TestComponent : ComponentBase
            {
                protected TestModel Model { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.AspNetCore.Components;
        using RxBlazorV2.Component;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestComponent : ObservableComponent<TestModel>
            {
                protected TestModel Model { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveObservableModelAttributes_RemovesAllModelAttributes()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.AspNetCore.Components;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.ComponentNotObservableError.Id}}:ObservableModelScope(ModelScope.Singleton)|}]
            [ObservableModelReference<TestModel>]
            public partial class TestComponent : ComponentBase
            {
                protected TestModel Model { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.AspNetCore.Components;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class TestComponent : ComponentBase
            {
                protected TestModel Model { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

}
