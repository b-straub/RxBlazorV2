using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier =
    RxBlazorV2Test.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.CircularTriggerReferenceCodeFixProvider>;

namespace RxBlazorV2Test.AnalyzerAndCodefixTests;

public class CircularTriggerReferenceCodeFixTests
{
    [Fact]
    public async Task RemoveCircularTrigger_RemovesOnlyCircularAttribute()
    {
        // lang=csharp
        var test = @$"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {{
        public partial int Counter {{ get; set; }}
        
        [ObservableCommand(nameof(IncrementCounter))]
        [{{|{DiagnosticDescriptors.CircularTriggerReferenceError.Id}:ObservableCommandTrigger(nameof(Counter))|}}]
        public partial IObservableCommand IncrementCommand {{ get; }}
        
        private void IncrementCounter()
        {{
            Counter++; // This modifies the same property that triggers the command
        }}
    }}
}}";

        // lang=csharp
        var fixedCode = @"
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
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveCircularTrigger_PreservesOtherTriggers()
    {
        // lang=csharp
        var test = @$"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {{
        public partial int Counter1 {{ get; set; }}
        public partial int Counter2 {{ get; set; }}
        
        [ObservableCommand(nameof(IncrementCounter2))]
        [ObservableCommandTrigger(nameof(Counter1))]
        [{{|{DiagnosticDescriptors.CircularTriggerReferenceError.Id}:ObservableCommandTrigger(nameof(Counter2))|}}]
        public partial IObservableCommand IncrementCommand {{ get; }}
        
        private void IncrementCounter2()
        {{
            Counter2++; // This modifies Counter2, which triggers the command
        }}
    }}
}}";

        // lang=csharp
        var fixedCode = @"
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
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveCircularTrigger_ParametrizedCommand()
    {
        // lang=csharp
        var test = @$"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {{
        public partial int Counter {{ get; set; }}
        
        [ObservableCommand(nameof(AddToCounter))]
        [{{|{DiagnosticDescriptors.CircularTriggerReferenceError.Id}:ObservableCommandTrigger<int>(nameof(Counter), 5)|}}]
        public partial IObservableCommand<int> AddCommand {{ get; }}
        
        private void AddToCounter(int value)
        {{
            Counter += value; // This modifies the same property that triggers the command
        }}
    }}
}}";

        // lang=csharp
        var fixedCode = @"
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
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }
}