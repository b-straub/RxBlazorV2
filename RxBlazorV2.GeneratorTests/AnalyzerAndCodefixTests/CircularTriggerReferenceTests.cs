using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class CircularTriggerReferenceTests
{
    [Fact]
    public async Task CommandTriggerModifiesSameProperty_CircularReferenceErrorExpected()
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
                public partial int Counter { get; set; }
                             
                [ObservableCommand(nameof(IncrementCounter))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Counter))|}]
                public partial IObservableCommand IncrementCommand { get; }
                             
                private void IncrementCounter()
                {
                    Counter++; // This modifies the same property that triggers the command
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandTriggerModifiesSamePropertyWithAssignment_CircularReferenceErrorExpected()
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
                public partial int Counter { get; set; }
                             
                [ObservableCommand(nameof(ResetCounter))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Counter))|}]
                public partial IObservableCommand ResetCommand { get; }
                             
                private void ResetCounter()
                {
                    Counter = 0; // This modifies the same property that triggers the command
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ParametrizedCommandTriggerModifiesSameProperty_CircularReferenceErrorExpected()
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
                public partial int Counter { get; set; }
                             
                [ObservableCommand(nameof(AddToCounter))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger<int>(nameof(Counter), 5)|}]
                public partial IObservableCommand<int> AddCommand { get; }
                             
                private void AddToCounter(int value)
                {
                    Counter += value; // This modifies the same property that triggers the command
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandTriggerModifiesDifferentProperty_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial int Counter { get; set; }
                public partial string Message { get; set; } = "";
                           
                [ObservableCommand(nameof(UpdateMessage))]
                [ObservableCommandTrigger(nameof(Counter))]
                public partial IObservableCommand UpdateMessageCommand { get; }
                           
                private void UpdateMessage()
                {
                    Message = $"Counter is now: {Counter}"; // This modifies a different property
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncCommandTriggerModifiesSameProperty_CircularReferenceErrorExpected()
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
                public partial string Status { get; set; } = "";
                             
                [ObservableCommand(nameof(UpdateStatusAsync))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Status))|}]
                public partial IObservableCommandAsync UpdateCommand { get; }
                             
                private async Task UpdateStatusAsync()
                {
                    await Task.Delay(100);
                    Status = "Updated"; // This modifies the same property that triggers the command
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandModifiesPropertyViaMemberAccess_CircularReferenceErrorExpected()
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
                public partial int Value { get; set; }
                             
                [ObservableCommand(nameof(ModifyValue))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Value))|}]
                public partial IObservableCommand ModifyCommand { get; }
                             
                private void ModifyValue()
                {
                    this.Value = 42; // This modifies the same property via member access
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithMultipleTriggers_OnlyCircularTriggerHasError()
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
                public partial int Counter1 { get; set; }
                public partial int Counter2 { get; set; }
                             
                [ObservableCommand(nameof(IncrementCounter2))]
                [ObservableCommandTrigger(nameof(Counter1))] // This is OK, different property
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Counter2))|}] // This creates circular reference
                public partial IObservableCommand IncrementCommand { get; }
                             
                private void IncrementCounter2()
                {
                    Counter2++; // This modifies Counter2, which triggers the command
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}