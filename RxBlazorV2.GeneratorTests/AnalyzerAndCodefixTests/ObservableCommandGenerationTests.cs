using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2Generator.Diagnostics;
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

    [Fact]
    public async Task CommandWithReturnValue_NoErrorsExpected()
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

                [ObservableCommand(nameof(IncrementSync))]
                public partial IObservableCommandR<int> IncrementCommand { get; }

                private int IncrementSync()
                {
                    Counter++;
                    return Counter * 10;
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncCommandWithReturnValue_NoErrorsExpected()
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

                [ObservableCommand(nameof(IncrementAsync))]
                public partial IObservableCommandRAsync<int> IncrementAsyncCommand { get; }

                private async Task<int> IncrementAsync()
                {
                    await Task.Delay(100);
                    Counter++;
                    return Counter * 10;
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task CommandReturnsValueButTypeDoesNotSupportIt_ErrorExpected()
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

                [ObservableCommand(nameof(IncrementSync))]
                public partial IObservableCommand IncrementCommand { get; }

                private int IncrementSync()
                {
                    Counter++;
                    return Counter * 10;
                }
            }
        }
        """;

        // NOTE: A compiler error (CS0407) will also occur in generated code due to signature mismatch
        var expected = new[]
        {
            AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.CommandMethodReturnsValueError)
                .WithLocation(13, 24)
                .WithArguments("IncrementCommand", "IObservableCommand", "IncrementSync", "int", "void"),
        };
        
        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected, "CS9248");
    }

    [Fact]
    public async Task AsyncCommandReturnsValueButTypeDoesNotSupportIt_ErrorExpected()
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
                [ObservableCommand(nameof(LoadDataAsync))]
                public partial IObservableCommandAsync LoadCommand { get; }

                private async Task<string> LoadDataAsync()
                {
                    await Task.Delay(1000);
                    return "Data loaded";
                }
            }
        }
        """;
        
        var expected = new[]
        {
            AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.CommandMethodReturnsValueError)
                .WithLocation(11, 24)
                .WithArguments("LoadCommand", "IObservableCommandAsync", "LoadDataAsync", "string", "IObservableCommandR<string>", "IObservableCommandRAsync<string>"),
        };
        
        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected, "CS9248");
    }

    [Fact]
    public async Task CommandExpectsReturnValueButMethodDoesNotProvideIt_ErrorExpected()
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

                [ObservableCommand(nameof(IncrementSync))]
                public partial IObservableCommandR<int> IncrementCommand { get; }

                private void IncrementSync()
                {
                    Counter++;
                }
            }
        }
        """;

        // Also expect compiler error for delegate signature mismatch
        var expected = new[]
        {
            AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.CommandMethodMissingReturnValueError)
                .WithLocation(13, 24)
                .WithArguments("IncrementCommand", "IObservableCommandR<int>", "int", "IncrementSync", "void", "int"),
        };

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected, "CS9248");
    }

    [Fact]
    public async Task AsyncCommandExpectsReturnValueButMethodDoesNotProvideIt_ErrorExpected()
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
                [ObservableCommand(nameof(LoadDataAsync))]
                public partial IObservableCommandRAsync<string> LoadCommand { get; }

                private async Task LoadDataAsync()
                {
                    await Task.Delay(1000);
                }
            }
        }
        """;

        // Also expect compiler error for delegate signature mismatch
        var expected = new[]
        {
            AnalyzerVerifier.Diagnostic(DiagnosticDescriptors.CommandMethodMissingReturnValueError)
                .WithLocation(11, 24)
                .WithArguments("LoadCommand", "IObservableCommandRAsync<string>", "string", "LoadDataAsync", "Task", "string"),
        };

        await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected, "CS9248");
    }

    [Fact]
    public async Task ParameterizedCommandWithReturnValue_NoErrorsExpected()
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
                [ObservableCommand(nameof(Calculate))]
                public partial IObservableCommandR<int, double> CalculateCommand { get; }

                private double Calculate(int value)
                {
                    return value * 1.5;
                }
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}