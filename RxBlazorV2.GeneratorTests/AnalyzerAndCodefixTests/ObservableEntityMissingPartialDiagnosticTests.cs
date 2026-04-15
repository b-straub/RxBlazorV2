using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class ObservableEntityMissingPartialDiagnosticTests
{
    [Fact]
    public async Task NonPartialObservableModelClass_ReportsError()
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

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Class", "TestModel", "inherits from ObservableModel", "class");
        
        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected, "CS0534");
    }

    [Fact]
    public async Task PartialObservableModelClass_NoError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NonPartialCommandProperty_ReportsError()
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

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "MyCommand", "implements IObservableCommand", "property");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PartialCommandProperty_NoError()
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
                public partial IObservableCommand MyCommand { get; }

                private void Execute() { }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NonPartialAsyncCommandProperty_ReportsError()
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
                [ObservableCommand(nameof(ExecuteAsync))]
                public IObservableCommandAsync {|#0:MyCommandAsync|} { get; }

                private async Task ExecuteAsync() { }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "MyCommandAsync", "implements IObservableCommand", "property");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonPartialClass_OnlyReportsClassError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public class {|#0:TestModel|} : ObservableModel
            {
                [ObservableCommand(nameof(Execute))]
                public IObservableCommand MyCommand { get; }

                private void Execute() { }
            }
        }
        """;

        // Only class diagnostic - command property check is skipped since class is non-partial
        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Class", "TestModel", "inherits from ObservableModel", "class");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected, "CS0534");
    }

    [Fact]
    public async Task NonPartialPropertyWithObservableTrigger_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableTrigger(nameof(OnNameChanged))]
                public string {|#0:Name|} { get; set; } = "";

                private void OnNameChanged() { }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "Name", "has [ObservableTrigger] attribute", "property");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonPartialPropertyWithObservableComponentTrigger_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public string {|#0:Theme|} { get; set; } = "Light";
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "Theme", "has [ObservableComponentTrigger] attribute", "property");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonPartialPropertyWithObservableComponentTriggerAsync_ReportsError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTriggerAsync]
                public bool {|#0:IsLoading|} { get; set; }
            }
        }
        """;

        var expected = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "IsLoading", "has [ObservableComponentTriggerAsync] attribute", "property");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PartialPropertyWithObservableTrigger_NoError()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                [ObservableTrigger(nameof(OnNameChanged))]
                public partial string Name { get; set; }

                private void OnNameChanged() { }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleNonPartialCommandProperties_ReportsMultipleErrors()
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

        var expected1 = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(0)
            .WithArguments("Property", "Command1", "implements IObservableCommand", "property");

        var expected2 = AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.ObservableEntityMissingPartialModifierError)
            .WithLocation(1)
            .WithArguments("Property", "Command2", "implements IObservableCommand", "property");

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, [expected1, expected2]);
    }
}

