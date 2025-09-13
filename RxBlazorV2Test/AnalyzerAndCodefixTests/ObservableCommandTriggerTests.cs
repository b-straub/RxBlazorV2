using RxBlazorV2Generator.Diagnostics;

using AnalyzerVerifier =
    RxBlazorV2Test.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2Test.Tests;

public class ObservableCommandTriggerTests
{
    [Fact]
    public async Task BasicCommandWithTrigger_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        
        [ObservableCommand(nameof(ExecuteMethod))]
        [ObservableCommandTrigger(nameof(Name))]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}"");
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithMultiplePropertyTriggers_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        public partial int Count { get; set; }
        
        [ObservableCommand(nameof(ExecuteMethod))]
        [ObservableCommandTrigger(nameof(Name))]
        [ObservableCommandTrigger(nameof(Count))]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}, Count: {Count}"");
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncCommandWithTrigger_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        
        [ObservableCommand(nameof(ExecuteAsyncMethod))]
        [ObservableCommandTrigger(nameof(Name))]
        public partial IObservableCommandAsync TestCommand { get; }
        
        private async Task ExecuteAsyncMethod()
        {
            await Task.Delay(100);
            Console.WriteLine($""Executed with Name: {Name}"");
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ParametrizedCommandWithTrigger_TriggerTypeArgumentsMismatchErrorExpected()
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
        public partial string Name {{ get; set; }} = """";
        
        [ObservableCommand(nameof(ExecuteMethodWithParam))]
        [{{|{DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id}:ObservableCommandTrigger<int>(nameof(Name), 3)|}}]
        public partial IObservableCommand<string> TestCommand {{ get; }}
        
        private void ExecuteMethodWithParam(string parameter)
        {{
            Console.WriteLine($""Executed with Name: {{Name}}, Parameter: {{parameter}}"");
        }}
    }}
}}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
    
    [Fact]
    public async Task ParametrizedCommandWithTrigger_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        
        [ObservableCommand(nameof(ExecuteMethodWithParam))]
        [ObservableCommandTrigger<string>(nameof(Name), ""NewTest"")]
        public partial IObservableCommand<string> TestCommand { get; }
        
        private void ExecuteMethodWithParam(string parameter)
        {
            Console.WriteLine($""Executed with Name: {Name}, Parameter: {parameter}"");
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithCanExecuteAndTrigger_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        public partial bool IsEnabled { get; set; }
        
        [ObservableCommand(nameof(ExecuteMethod), nameof(CanExecuteMethod))]
        [ObservableCommandTrigger(nameof(Name))]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}"");
        }
        
        private bool CanExecuteMethod()
        {
            return IsEnabled && !string.IsNullOrEmpty(Name);
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithCustomCanTriggerMethod_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        public partial bool ShouldTrigger { get; set; }
        
        [ObservableCommand(nameof(ExecuteMethod))]
        [ObservableCommandTrigger(nameof(Name), nameof(CanTriggerMethod))]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}"");
        }
        
        private bool CanTriggerMethod()
        {
            return ShouldTrigger && !string.IsNullOrEmpty(Name);
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithBufferedTrigger_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        
        [ObservableCommand(nameof(ExecuteMethod))]
        [ObservableCommandTrigger(nameof(Name))]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}"");
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithMultipleTriggerProperties_NoErrorsExpected()
    {
        // lang=csharp
        var test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string SearchText { get; set; } = """";
        public partial bool IsEnabled { get; set; }
        
        [ObservableCommand(nameof(SearchMethod))]
        [ObservableCommandTrigger(nameof(SearchText))]
        [ObservableCommandTrigger(nameof(IsEnabled))]
        public partial IObservableCommandAsync SearchCommand { get; }
        
        private async Task SearchMethod()
        {
            // Simulate search operation
            await Task.Delay(100);
            Console.WriteLine($""Searching for: {SearchText}"");
        }
    }
}";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}