using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class ObservableCommandGenerationTests
{
    [Fact]
    public async Task BasicSyncCommand_NoErrorsExpected()
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
                           [ObservableCommand(nameof(ExecuteMethod))]
                           public partial IObservableCommand TestCommand { get; }
                           
                           private void ExecuteMethod()
                           {
                               Console.WriteLine("Command executed");
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task BasicAsyncCommand_NoErrorsExpected()
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
                           [ObservableCommand(nameof(ExecuteAsyncMethod))]
                           public partial IObservableCommandAsync TestCommand { get; }
                           
                           private async Task ExecuteAsyncMethod()
                           {
                               await Task.Delay(100);
                               Console.WriteLine("Async command executed");
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ParametrizedSyncCommand_NoErrorsExpected()
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
                           [ObservableCommand(nameof(ExecuteMethodWithParam))]
                           public partial IObservableCommand<string> TestCommand { get; }
                           
                           private void ExecuteMethodWithParam(string parameter)
                           {
                               Console.WriteLine($"Command executed with parameter: {parameter}");
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ParametrizedAsyncCommand_NoErrorsExpected()
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
                           [ObservableCommand(nameof(ExecuteAsyncMethodWithParam))]
                           public partial IObservableCommandAsync<int> TestCommand { get; }
                           
                           private async Task ExecuteAsyncMethodWithParam(int parameter)
                           {
                               await Task.Delay(parameter);
                               Console.WriteLine($"Async command executed with parameter: {parameter}");
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandWithCanExecute_NoErrorsExpected()
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
                           public partial bool IsEnabled { get; set; }
                           
                           [ObservableCommand(nameof(ExecuteMethod), nameof(CanExecuteMethod))]
                           public partial IObservableCommand TestCommand { get; }
                           
                           private void ExecuteMethod()
                           {
                               Console.WriteLine("Command executed");
                           }
                           
                           private bool CanExecuteMethod()
                           {
                               return IsEnabled;
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncCommandWithCancellation_NoErrorsExpected()
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
                           [ObservableCommand(nameof(ExecuteAsyncMethodWithCancellation))]
                           public partial IObservableCommandAsync<string> TestCommand { get; }
                           
                           private async Task ExecuteAsyncMethodWithCancellation(string parameter, CancellationToken cancellationToken)
                           {
                               await Task.Delay(1000, cancellationToken);
                               Console.WriteLine($"Command executed with parameter: {parameter}");
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleCommandsInSameModel_NoErrorsExpected()
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
                           [ObservableCommand(nameof(FirstMethod))]
                           public partial IObservableCommand FirstCommand { get; }
                           
                           [ObservableCommand(nameof(SecondMethod))]
                           public partial IObservableCommandAsync SecondCommand { get; }
                           
                           [ObservableCommand(nameof(ThirdMethod))]
                           public partial IObservableCommand<int> ThirdCommand { get; }
                           
                           private void FirstMethod()
                           {
                               Console.WriteLine("First command executed");
                           }
                           
                           private async Task SecondMethod()
                           {
                               await Task.Delay(100);
                               Console.WriteLine("Second command executed");
                           }
                           
                           private void ThirdMethod(int value)
                           {
                               Console.WriteLine($"Third command executed with value: {value}");
                           }
                       }
                   }
                   """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}