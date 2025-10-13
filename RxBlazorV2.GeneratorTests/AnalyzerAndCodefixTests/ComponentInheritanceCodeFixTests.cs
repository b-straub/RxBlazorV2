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

            public partial class {|{{DiagnosticDescriptors.ComponentNotObservableWarning.Id}}:TestComponent|} : ComponentBase
            {
                [Inject]
                public required TestModel Model { get; init; }
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

            public partial class TestComponent : ObservableComponent
            {
                [Inject]
                public required TestModel Model { get; init; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ChangeToObservableComponent_FixesOwningComponentBaseInheritance()
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

            public partial class {|{{DiagnosticDescriptors.ComponentNotObservableWarning.Id}}:TestComponent|} : OwningComponentBase
            {
                [Inject]
                public required TestModel Model { get; init; }
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

            public partial class TestComponent : ObservableComponent
            {
                [Inject]
                public required TestModel Model { get; init; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ChangeToObservableComponent_FixesOwningComponentBaseGenericInheritance()
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

            public partial class {|{{DiagnosticDescriptors.ComponentNotObservableWarning.Id}}:TestComponent|} : OwningComponentBase<TestModel>
            {
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

            public partial class TestComponent : ObservableComponent<TestModel>
            {
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }
}
