using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for ObservableTrigger attribute - automatically executes methods when properties change
/// </summary>
public class ObservableTriggerTests
{
    [Fact]
    public async Task BasicPropertyWithSingleTrigger_NoErrorsExpected()
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
                [ObservableTrigger(nameof(ValidateInput))]
                public partial string Input { get; set; } = "";

                private void ValidateInput()
                {
                    Console.WriteLine($"Validating: {Input}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PropertyWithMultipleTriggers_NoErrorsExpected()
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
                [ObservableTrigger(nameof(UpdateValidation))]
                [ObservableTrigger(nameof(SaveChanges))]
                public partial string Name { get; set; } = "";

                private void UpdateValidation()
                {
                    Console.WriteLine($"Validating: {Name}");
                }

                private void SaveChanges()
                {
                    Console.WriteLine($"Saving: {Name}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncTrigger_NoErrorsExpected()
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
                [ObservableTrigger(nameof(SaveAsync))]
                public partial string Data { get; set; } = "";

                private async Task SaveAsync()
                {
                    await Task.Delay(100);
                    Console.WriteLine($"Saved: {Data}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TriggerWithCanTriggerMethod_NoErrorsExpected()
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
                [ObservableTrigger(nameof(UpdateValue), nameof(CanUpdate))]
                public partial int Count { get; set; }

                private void UpdateValue()
                {
                    Console.WriteLine($"Count updated: {Count}");
                }

                private bool CanUpdate()
                {
                    return Count > 0;
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ParametrizedTrigger_NoErrorsExpected()
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
                [ObservableTrigger<string>(nameof(LogChange), "PropertyChanged")]
                public partial int Value { get; set; }

                private void LogChange(string message)
                {
                    Console.WriteLine($"{message}: {Value}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CircularTrigger_ErrorExpected()
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
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableTrigger(nameof(UpdateCounter))|}]
                public partial int Counter { get; set; }

                private void UpdateCounter()
                {
                    Counter++;  // Circular: modifies the same property
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultiplePropertiesWithTriggers_NoErrorsExpected()
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
                [ObservableTrigger(nameof(ValidateFirstName))]
                public partial string FirstName { get; set; } = "";

                [ObservableTrigger(nameof(ValidateLastName))]
                public partial string LastName { get; set; } = "";

                private void ValidateFirstName()
                {
                    Console.WriteLine($"Validating first name: {FirstName}");
                }

                private void ValidateLastName()
                {
                    Console.WriteLine($"Validating last name: {LastName}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MixedSyncAndAsyncTriggers_NoErrorsExpected()
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
                [ObservableTrigger(nameof(SyncValidation))]
                [ObservableTrigger(nameof(AsyncSave))]
                public partial string Data { get; set; } = "";

                private void SyncValidation()
                {
                    Console.WriteLine($"Sync validation: {Data}");
                }

                private async Task AsyncSave()
                {
                    await Task.Delay(100);
                    Console.WriteLine($"Async saved: {Data}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ProtectedPropertyWithTrigger_NoErrorsExpected()
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
                [ObservableTrigger(nameof(OnInternalChange))]
                protected partial int InternalValue { get; set; }

                private void OnInternalChange()
                {
                    Console.WriteLine($"Internal value changed: {InternalValue}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TriggerWithCancellationSupport_NoErrorsExpected()
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
                [ObservableTrigger(nameof(ProcessAsync))]
                public partial string Input { get; set; } = "";

                private async Task ProcessAsync(CancellationToken cancellationToken)
                {
                    await Task.Delay(1000, cancellationToken);
                    Console.WriteLine($"Processed: {Input}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ParametrizedAsyncTrigger_NoErrorsExpected()
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
                [ObservableTrigger<string>(nameof(NotifyAsync), "ChangeDetected")]
                public partial int Status { get; set; }

                private async Task NotifyAsync(string message)
                {
                    await Task.Delay(50);
                    Console.WriteLine($"{message}: Status = {Status}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TriggerWithCanTriggerAndAsync_NoErrorsExpected()
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
                [ObservableTrigger(nameof(SaveAsync), nameof(CanSave))]
                public partial string Data { get; set; } = "";

                private async Task SaveAsync()
                {
                    await Task.Delay(100);
                    Console.WriteLine($"Saved: {Data}");
                }

                private bool CanSave()
                {
                    return !string.IsNullOrEmpty(Data);
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleTriggersOnDifferentProperties_NoErrorsExpected()
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
                [ObservableTrigger(nameof(OnFirstNameChange))]
                public partial string FirstName { get; set; } = "";

                [ObservableTrigger(nameof(OnLastNameChange))]
                public partial string LastName { get; set; } = "";

                [ObservableTrigger(nameof(OnAgeChange))]
                public partial int Age { get; set; }

                private void OnFirstNameChange()
                {
                    Console.WriteLine($"First name: {FirstName}");
                }

                private void OnLastNameChange()
                {
                    Console.WriteLine($"Last name: {LastName}");
                }

                private void OnAgeChange()
                {
                    Console.WriteLine($"Age: {Age}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TriggersAndCommandsInSameModel_NoErrorsExpected()
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
                [ObservableTrigger(nameof(ValidateInput))]
                public partial string Input { get; set; } = "";

                [ObservableCommand(nameof(SaveExecute))]
                public partial IObservableCommand SaveCommand { get; }

                private void ValidateInput()
                {
                    Console.WriteLine($"Validating: {Input}");
                }

                private void SaveExecute()
                {
                    Console.WriteLine($"Saving: {Input}");
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
