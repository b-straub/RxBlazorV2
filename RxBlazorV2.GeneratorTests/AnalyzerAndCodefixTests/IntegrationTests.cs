using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class IntegrationTests
{
    [Fact]
    public async Task DetectMultipleDiagnostics_AcrossDifferentModels()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class InvalidService
            {
                public string Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB))|}]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelB : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference(typeof(InvalidService))|}]
            public partial class ModelC : ObservableModel
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectMultipleDiagnostics_GenericConstraintInMultipleModels()
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
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB<>))|}]
            public partial class ModelA<T> : ObservableModel where T : struct
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA<>))|}]
            public partial class ModelB<T> : ObservableModel where T : struct
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModel<>))|}]
            public partial class ModelC<T> : ObservableModel where T : struct
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectMultipleDiagnostics_ScopeAndCircular()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Component;

        namespace Test
        {
            [{|{{DiagnosticDescriptors.SharedModelNotSingletonError.Id}}:ObservableModelScope(ModelScope.Scoped)|}]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelB))|}]
            public partial class ModelA : ObservableModel
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.CircularModelReferenceError.Id}}:ObservableModelReference(typeof(ModelA))|}]
            public partial class ModelB : ObservableModel
            {
            }

            public partial class ComponentA : ObservableComponent<ModelA>
            {
            }

            public partial class ComponentB : ObservableComponent<ModelA>
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectMultipleDiagnostics_TriggerTypeAndCircularTrigger()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; } = "";
                public partial int Counter { get; set; }

                [ObservableCommand(nameof(Execute))]
                [{|{{DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id}}:ObservableCommandTrigger<int>(nameof(Name), 0)|}]
                public partial IObservableCommand<string> TestCommand { get; }

                [ObservableCommand(nameof(IncrementCounter))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Counter))|}]
                public partial IObservableCommand IncrementCommand { get; }

                private void Execute(string param) { }

                private void IncrementCounter()
                {
                    Counter++;
                }
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DetectMultipleDiagnostics_ArityAndConstraintMismatch()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModelTwoParams<T, U> : ObservableModel where T : class where U : struct
            {
                public partial T Value1 { get; set; }
                public partial U Value2 { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModelConstraint<T> : ObservableModel where T : class, new()
            {
                public partial T Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.GenericArityMismatchError.Id}}:ObservableModelReference(typeof(GenericModelTwoParams<,>))|}]
            public partial class TestModel1<T> : ObservableModel where T : class
            {
            }

            [ObservableModelScope(ModelScope.Singleton)]
            [{|{{DiagnosticDescriptors.TypeConstraintMismatchError.Id}}:ObservableModelReference(typeof(GenericModelConstraint<>))|}]
            public partial class TestModel2<T> : ObservableModel where T : class
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
