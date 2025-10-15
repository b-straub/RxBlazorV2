using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.UnusedModelReferenceCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class UnusedModelReferenceDiagnosticTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelReferenceWithUsedProperties_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
                public partial int Counter2 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(CounterModel counterModel);

                public partial int Value { get; set; }

                // Uses Counter1 from CounterModel
                public int Total => Value + CounterModel.Counter1;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelReferenceWithNoUsedProperties_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
                public partial int Counter2 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:CounterModel|} counterModel);

                public partial int Value { get; set; }

                // Does NOT use any properties from CounterModel
                public void DoSomething()
                {
                    Value = 42;
                }
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelReferenceWithNoUsedProperties_CodeFixRemovesAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
                public partial int Counter2 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:CounterModel|} counterModel);

                public partial int Value { get; set; }

                public void DoSomething()
                {
                    Value = 42;
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
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
                public partial int Counter2 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial int Value { get; set; }

                public void DoSomething()
                {
                    Value = 42;
                }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task MultipleModelReferences_OneUnused_OnlyUnusedMarkedWithDiagnostic()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class OtherModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(CounterModel counterModel, {|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:OtherModel|} otherModel);

                public partial int Value { get; set; }

                // Uses Counter1 but not OtherModel.Name
                public int Total => Value + CounterModel.Counter1;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleModelReferences_OneUnused_CodeFixRemovesOnlyUnusedAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class OtherModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(CounterModel counterModel, {|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:OtherModel|} otherModel);

                public partial int Value { get; set; }

                public int Total => Value + CounterModel.Counter1;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class OtherModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(CounterModel counterModel);

                public partial int Value { get; set; }

                public int Total => Value + CounterModel.Counter1;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task UnusedModelReference_WithComplexPropertyPattern_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
                public partial int Counter2 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:CounterModel|} counterModel);

                public partial bool AddMode { get; set; }

                // Property doesn't actually use CounterModel properties (only references AddMode)
                public bool IsValid => AddMode;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ModelReferenceUsedInCommand_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Counter1 { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(CounterModel counterModel);

                public partial int Value { get; set; }

                [ObservableCommand(nameof(Execute))]
                public partial IObservableCommand TestCommand { get; }

                // Uses Counter1 in command method
                private void Execute()
                {
                    Value = CounterModel.Counter1 * 2;
                }
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }
}
