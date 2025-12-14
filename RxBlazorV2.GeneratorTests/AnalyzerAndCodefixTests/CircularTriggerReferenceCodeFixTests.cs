using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.CircularTriggerReferenceCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG031 (Circular trigger reference) code fix functionality.
///
/// For ObservableTrigger/ObservableCommandTrigger (attribute-based):
///   - Option 0: Remove the trigger attribute
///   - Option 1: Remove the property modification in execute method
///
/// For Internal Model Observers (statement-based):
///   - Option 0: Remove the observer method
///   - Option 1: Remove the property modification statement
/// </summary>
public class CircularTriggerReferenceCodeFixTests
{
    #region ObservableCommandTrigger - Remove Attribute (Option 0)

    [Fact]
    public async Task ObservableCommandTrigger_RemoveAttribute()
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

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial int Counter { get; set; }

                [ObservableCommand(nameof(IncrementCounter))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter()
                {
                    Counter++; // This modifies the same property that triggers the command
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task ObservableCommandTrigger_RemoveAttribute_PreservesOtherTriggers()
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
                [ObservableCommandTrigger(nameof(Counter1))]
                [{|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:ObservableCommandTrigger(nameof(Counter2))|}]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter2()
                {
                    Counter2++; // This modifies Counter2, which triggers the command
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
                [ObservableCommandTrigger(nameof(Counter1))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter2()
                {
                    Counter2++; // This modifies Counter2, which triggers the command
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task ObservableCommandTrigger_RemoveAttribute_ParametrizedCommand()
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

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial int Counter { get; set; }

                [ObservableCommand(nameof(AddToCounter))]
                public partial IObservableCommand<int> AddCommand { get; }

                private void AddToCounter(int value)
                {
                    Counter += value; // This modifies the same property that triggers the command
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    #endregion

    #region ObservableCommandTrigger - Remove Property Modification (Option 1)

    [Fact]
    public async Task ObservableCommandTrigger_RemovePropertyModification()
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
                    Counter++; // This will be removed
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial int Counter { get; set; }

                [ObservableCommand(nameof(IncrementCounter))]
                [ObservableCommandTrigger(nameof(Counter))]
                public partial IObservableCommand IncrementCommand { get; }

                private void IncrementCounter()
                {
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task ObservableCommandTrigger_RemovePropertyModification_AssignmentExpression()
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
                    Counter = 0; // Assignment will be removed
                    System.Console.WriteLine("Reset done");
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                public partial int Counter { get; set; }

                [ObservableCommand(nameof(ResetCounter))]
                [ObservableCommandTrigger(nameof(Counter))]
                public partial IObservableCommand ResetCommand { get; }

                private void ResetCounter()
                {
                    System.Console.WriteLine("Reset done");
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    #endregion

    #region Internal Model Observer - Remove Method (Option 0)

    [Fact]
    public async Task InternalModelObserver_RemoveMethod()
    {
        // Diagnostic is now on the method identifier (not on the statement)
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TimerModel : ObservableModel
            {
                public partial bool IsRunning { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(TimerModel timer);

                // This method observes Timer.IsRunning and also modifies it - circular reference!
                private void {|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:OnTimerStateChanged|}()
                {
                    Timer.IsRunning = false; // Modifies the observed property
                    var state = Timer.IsRunning ? "Running" : "Stopped";
                }

                // Another observer that uses Timer - ensures Timer is still used after removing the circular one
                private void OnTimerRunningChanged()
                {
                    System.Console.WriteLine($"Timer is: {Timer.IsRunning}");
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TimerModel : ObservableModel
            {
                public partial bool IsRunning { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(TimerModel timer);

                // Another observer that uses Timer - ensures Timer is still used after removing the circular one
                private void OnTimerRunningChanged()
                {
                    System.Console.WriteLine($"Timer is: {Timer.IsRunning}");
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    #endregion

    #region Internal Model Observer - Remove Property Modification (Option 1)

    [Fact]
    public async Task InternalModelObserver_RemovePropertyModification()
    {
        // Diagnostic is now on the method identifier (not on the statement)
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TimerModel : ObservableModel
            {
                public partial bool IsRunning { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(TimerModel timer);

                // This method observes Timer.IsRunning and also modifies it - circular reference!
                private void {|{{DiagnosticDescriptors.CircularTriggerReferenceError.Id}}:OnTimerStateChanged|}()
                {
                    Timer.IsRunning = false; // Modifies the observed property
                    var state = Timer.IsRunning ? "Running" : "Stopped";
                }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TimerModel : ObservableModel
            {
                public partial bool IsRunning { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(TimerModel timer);

                // This method observes Timer.IsRunning and also modifies it - circular reference!
                private void OnTimerStateChanged()
                {
                    var state = Timer.IsRunning ? "Running" : "Stopped";
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    #endregion
}
