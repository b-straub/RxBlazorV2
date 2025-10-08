using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests semantic attribute resolution vs string-based matching.
/// Validates that the AttributeExtensions helper methods correctly identify
/// attributes using Roslyn semantic model instead of string comparison.
/// </summary>
public class AttributeResolutionTests
{
    [Fact]
    public async Task ResolveAttribute_WithNamespace()
    {
        // Verifies semantic resolution works across namespaces
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test.SubNamespace
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }
        }

        namespace Test
        {
            using Test.SubNamespace;

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference<CounterModel>]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(IncrementCounter))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter()
                {
                    CounterModel.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveAttribute_WithoutAttributeSuffix()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference<CounterModel>]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(IncrementCounter))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter()
                {
                    CounterModel.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveAttribute_WithAttributeSuffix()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScopeAttribute(ModelScope.Singleton)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScopeAttribute(ModelScope.Singleton)]
            [ObservableModelReferenceAttribute<CounterModel>]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommandAttribute(nameof(IncrementCounter))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter()
                {
                    CounterModel.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveAttribute_CommandTrigger()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MainModel : ObservableModel
            {
                public partial int Source { get; set; }
                public partial int Target { get; set; }

                [ObservableCommand(nameof(UpdateTarget))]
                [ObservableCommandTrigger(nameof(Source))]
                public partial IObservableCommand UpdateCommand { get; }

                private void UpdateTarget()
                {
                    Target = Source * 2;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveAttribute_GenericCommandWithParameter()
    {
        // Verifies semantic resolution works for commands with generic parameters
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MainModel : ObservableModel
            {
                public partial int Counter { get; set; }

                [ObservableCommand(nameof(IncrementBy))]
                public partial IObservableCommand<int> IncrementCommand { get; }

                private void IncrementBy(int amount)
                {
                    Counter += amount;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveAttribute_MultipleAttributes()
    {
        // Verifies semantic resolution correctly identifies multiple different attributes
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class Counter1Model : ObservableModel
            {
                public partial int Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class Counter2Model : ObservableModel
            {
                public partial int Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference<Counter1Model>]
            [ObservableModelReference<Counter2Model>]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(IncrementBoth))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementBoth()
                {
                    Counter1Model.Value++;
                    Counter2Model.Value++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
