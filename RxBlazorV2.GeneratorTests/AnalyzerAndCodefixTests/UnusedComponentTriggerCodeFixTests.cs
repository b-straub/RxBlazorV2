using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<
    RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.UnusedComponentTriggerCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class UnusedComponentTriggerCodeFixTests
{
    [Fact]
    public async Task AddObservableComponentAttribute_FixesWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task RemoveTriggerAttribute_FixesWarning()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 1);
    }

    [Fact]
    public async Task AddObservableComponentAttribute_PreservesExistingAttributes()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [Obsolete]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using System;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [Obsolete]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task RemoveTriggerAttribute_WithBothSyncAndAsync_RemovesBoth()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                [ObservableComponentTriggerAsync]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 1);
    }

    [Fact]
    public async Task RemoveTriggerAttribute_PreservesOtherAttributes()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using System;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [Obsolete]
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using System;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [Obsolete]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 1);
    }

    [Fact]
    public async Task AddObservableComponentAttribute_WithComments_PreservesTrivia()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            // This is a test model
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            // This is a test model
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task AddObservableComponentAttribute_PreservesIndentation()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task AddObservableComponentAttribute_AddsUsingDirective()
    {
        // lang=csharp
        var test = """
        using System;

        namespace Test
        {
            [RxBlazorV2.Model.ObservableModelScope(RxBlazorV2.Model.ModelScope.Scoped)]
            public partial class TestModel : RxBlazorV2.Model.ObservableModel
            {
                [{|#0:RxBlazorV2.Model.ObservableComponentTrigger|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """
        using System;
        using RxBlazorV2.Model;

        namespace Test
        {
            [RxBlazorV2.Model.ObservableModelScope(RxBlazorV2.Model.ModelScope.Scoped)]
            [ObservableComponent]
            public partial class TestModel : RxBlazorV2.Model.ObservableModel
            {
                [RxBlazorV2.Model.ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 0);
    }

    [Fact]
    public async Task RemoveTriggerAttribute_WithCustomHookName_RemovesAttribute()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger(hookMethodName: "CustomHookName")|}]
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, 1);
    }

    [Fact]
    public async Task AddObservableComponentAttribute_MultiplePropertiesWithTriggers_FixesAll()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial string Name { get; set; }

                [{|#1:ObservableComponentTrigger|}]
                public partial int Count { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }

                [ObservableComponentTrigger]
                public partial int Count { get; set; }
            }
        }
        """;

        var expected1 = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(0)
            .WithArguments("Name", "TestModel");

        var expected2 = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning)
            .WithLocation(1)
            .WithArguments("Count", "TestModel");

        // Apply the code fix to the first diagnostic
        await CodeFixVerifier.VerifyCodeFixAsync(test, [expected1, expected2], fixedCode, 0);
    }
}
