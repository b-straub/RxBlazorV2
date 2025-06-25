using RxBlazorV2Generator.Diagnostics;

using CodeFixVerifier =
    RxBlazorV2Test.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.TriggerTypeArgumentsCodeFixProvider>;

using AnalyzerVerifier =
    RxBlazorV2Test.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2Test.Tests;

public class TriggerTypeArgumentsCodeFixTests
{
    [Fact]
    public async Task AnalyzerDetectsTriggerTypeArgumentsMismatch()
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
        [{|" + DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id + @":ObservableCommandTrigger<int>(nameof(Name), 42)|}]
        public partial IObservableCommand<string> TestCommand { get; }
        
        private void ExecuteMethodWithParam(string parameter)
        {
            Console.WriteLine(""Executed with Name: "" + Name + "", Parameter: "" + parameter);
        }
    }
}";

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task FixTriggerTypeArguments_MatchesCommandType()
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
        [{|" + DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id + @":ObservableCommandTrigger<int>(nameof(Name), 42)|}]
        public partial IObservableCommand<string> TestCommand { get; }
        
        private void ExecuteMethodWithParam(string parameter)
        {
            Console.WriteLine(""Executed with Name: "" + Name + "", Parameter: "" + parameter);
        }
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        
        [ObservableCommand(nameof(ExecuteMethodWithParam))]
        [ObservableCommandTrigger<string>(nameof(Name), null)]
        public partial IObservableCommand<string> TestCommand { get; }
        
        private void ExecuteMethodWithParam(string parameter)
        {
            Console.WriteLine(""Executed with Name: "" + Name + "", Parameter: "" + parameter);
        }
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveTriggerTypeArguments_UsesNonGenericVersion()
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
        [{|" + DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id + @":ObservableCommandTrigger<string>(nameof(Name), ""test"")|}]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}"");
        }
    }
}";

        // lang=csharp
        var fixedCode = @"
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
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }

    [Fact]
    public async Task FixAsyncCommandTriggerTypeArguments_MatchesCommandType()
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
        
        [ObservableCommand(nameof(ExecuteAsyncMethodWithParam))]
        [{|" + DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id + @":ObservableCommandTrigger<string>(nameof(Name), ""test"")|}]
        public partial IObservableCommandAsync<int> TestCommand { get; }
        
        private async Task ExecuteAsyncMethodWithParam(int parameter)
        {
            await Task.Delay(parameter);
            Console.WriteLine(""Executed with Name: "" + Name + "", Parameter: "" + parameter);
        }
    }
}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class TestModel : ObservableModel
    {
        public partial string Name { get; set; } = """";
        
        [ObservableCommand(nameof(ExecuteAsyncMethodWithParam))]
        [ObservableCommandTrigger<int>(nameof(Name), 0)]
        public partial IObservableCommandAsync<int> TestCommand { get; }
        
        private async Task ExecuteAsyncMethodWithParam(int parameter)
        {
            await Task.Delay(parameter);
            Console.WriteLine(""Executed with Name: "" + Name + "", Parameter: "" + parameter);
        }
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task FixNonGenericToGenericCommand_AddsCorrectTypeArguments()
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
        [{|" + DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id + @":ObservableCommandTrigger<string>(nameof(Name), ""test"")|}]
        public partial IObservableCommand TestCommand { get; }
        
        private void ExecuteMethod()
        {
            Console.WriteLine($""Executed with Name: {Name}"");
        }
    }
}";

        // lang=csharp
        var fixedCode = @"
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
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 1);
    }
}