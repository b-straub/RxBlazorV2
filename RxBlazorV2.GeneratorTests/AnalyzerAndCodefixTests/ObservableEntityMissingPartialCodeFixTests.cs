using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.ObservableEntityMissingPartialCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class ObservableEntityMissingPartialCodeFixTests
{
    [Fact]
    public async Task FixNonPartialClass_AddsPartialModifier()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public class {|#0:TestModel|} : ObservableModel
            {
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
            }
        }
        """;

        var expected = new[]
        {
            CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
                .WithLocation(0)
                .WithArguments("Class", "TestModel", "inherits from ObservableModel", "class"),
        };

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, null, "CS0534");
    }

    [Fact]
    public async Task FixNonPartialCommandProperty_AddsPartialModifier()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Execute))]
                public IObservableCommand {|#0:MyCommand|} { get; }

                private void Execute() { }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Execute))]
                public partial IObservableCommand MyCommand { get; }

                private void Execute() { }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "MyCommand", "implements IObservableCommand", "property");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }

    [Fact]
    public async Task FixMultipleNonPartialCommandProperties_FixAllAddsAllPartialModifiers()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Execute1))]
                public IObservableCommand {|#0:Command1|} { get; }

                [ObservableCommand(nameof(Execute2))]
                public IObservableCommandAsync {|#1:Command2|} { get; }

                private void Execute1() { }
                private async Task Execute2() { }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Execute1))]
                public partial IObservableCommand Command1 { get; }

                [ObservableCommand(nameof(Execute2))]
                public partial IObservableCommandAsync Command2 { get; }

                private void Execute1() { }
                private async Task Execute2() { }
            }
        }
        """;

        var expected1 = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "Command1", "implements IObservableCommand", "property");

        var expected2 = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(1)
            .WithArguments("Property", "Command2", "implements IObservableCommand", "property");

        await CodeFixVerifier.VerifyCodeFixAsync(test, [expected1, expected2], fixedCode);
    }

    [Fact]
    public async Task FixInternalClass_AddsPartialModifier()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            internal class {|#0:TestModel|} : ObservableModel
            {
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            internal partial class TestModel : ObservableModel
            {
            }
        }
        """;

        var expected = new[]
        {
            CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
                .WithLocation(0)
                .WithArguments("Class", "TestModel", "inherits from ObservableModel", "class")
        };

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode, null, "CS0534");
    }

    [Fact]
    public async Task FixSealedClass_AddsPartialAfterAccessibility()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public sealed class {|#0:TestModel|} : ObservableModel
            {
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public sealed partial class TestModel : ObservableModel
            {
            }
        }
        """;

        var expected = new[]
        {
            CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
                .WithLocation(0)
                .WithArguments("Class", "TestModel", "inherits from ObservableModel", "class")
          };

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode,  null, "CS0534");
    }

    [Fact]
    public async Task FixProtectedCommandProperty_AddsPartialModifier()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Execute))]
                protected IObservableCommand {|#0:MyCommand|} { get; }

                private void Execute() { }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableCommand(nameof(Execute))]
                protected partial IObservableCommand MyCommand { get; }

                private void Execute() { }
            }
        }
        """;

        var expected = CodeFixVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "MyCommand", "implements IObservableCommand", "property");

        await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
    }
}
