using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

public class SemanticResolutionTests
{
    [Fact]
    public async Task ResolveModelReference_WithQualifiedName()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test.Models
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
                    // Access model reference property
                    CounterModel.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_WithNestedNamespace()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test.Core.Models
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class NestedCounter : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }

        namespace Test.Core
        {
            using Test.Core.Models;

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference<NestedCounter>]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(IncrementNested))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementNested()
                {
                    NestedCounter.Value++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_ThroughLocalVariable()
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
                [ObservableCommand(nameof(ProcessCounter))]
                public partial IObservableCommand ProcessCommand { get; }

                private void ProcessCounter()
                {
                    var counter = CounterModel;
                    counter.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_ThroughParameter()
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
                [ObservableCommand(nameof(ProcessCounter))]
                public partial IObservableCommand ProcessCommand { get; }

                private void ProcessCounter()
                {
                    UpdateCounter(CounterModel);
                }

                private void UpdateCounter(CounterModel model)
                {
                    model.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_WithGenericModel()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericCounter<T> : ObservableModel where T : struct
            {
                public partial T Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference(typeof(GenericCounter<>))]
            public partial class MainModel<T> : ObservableModel where T : struct
            {
                [ObservableCommand(nameof(IncrementValue))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementValue()
                {
                    // Access generic model property
                    var current = GenericCounter.Value;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_WithPropertyChain()
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
                public partial string Name { get; set; }

                [ObservableCommand(nameof(ProcessData))]
                public partial IObservableCommand ProcessCommand { get; }

                private void ProcessData()
                {
                    // Access through property chain
                    this.CounterModel.Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_WithMultipleReferences()
    {
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
                [ObservableCommand(nameof(ProcessAll))]
                public partial IObservableCommand ProcessCommand { get; }

                private void ProcessAll()
                {
                    // Access multiple model references
                    Counter1Model.Value++;
                    Counter2Model.Value++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveModelReference_WithConditionalAccess()
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
                public partial int? Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableModelReference<CounterModel>]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(SafeIncrement))]
                public partial IObservableCommand IncrementCommand { get; }

                private void SafeIncrement()
                {
                    var current = CounterModel?.Counter ?? 0;
                    CounterModel.Counter = current + 1;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveCancellationToken_WithSystemThreadingNamespace()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(ExecuteAsync))]
                public partial IObservableCommandAsync ProcessCommand { get; }

                private async Task ExecuteAsync(CancellationToken cancellationToken)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ResolveCancellationToken_WithFullyQualifiedName()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Threading.Tasks;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MainModel : ObservableModel
            {
                [ObservableCommand(nameof(ExecuteAsync))]
                public partial IObservableCommandAsync ProcessCommand { get; }

                private async Task ExecuteAsync(System.Threading.CancellationToken cancellationToken)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
