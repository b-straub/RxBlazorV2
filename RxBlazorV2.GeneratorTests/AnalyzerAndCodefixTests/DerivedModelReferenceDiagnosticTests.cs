using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.DerivedModelReferenceCodeFixProvider>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class DerivedModelReferenceDiagnosticTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DirectObservableModelInheritance_NoErrorsExpected()
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

                public int Total => Value + CounterModel.Counter1;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ReferenceDerivedModel_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public abstract string Usage { get; }
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public override string Usage => "Example";
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.DerivedModelReferenceError.Id}}:DerivedModel|} derivedModel);

                public partial int Value { get; set; }
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ReferenceDerivedModel_CodeFixRemovesAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public abstract string Usage { get; }
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public override string Usage => "Example";
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.DerivedModelReferenceError.Id}}:DerivedModel|} derivedModel);

                public partial int Value { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public abstract string Usage { get; }
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public override string Usage => "Example";
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task ReferenceAbstractBaseModel_GetsRXBG052()
    {
        // Referencing an abstract base model should give RXBG052 (abstract class cannot be instantiated by DI)
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public abstract string Usage { get; }
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public override string Usage => "Example";
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.AbstractClassInPartialConstructorError.Id}}:BaseModel|} baseModel);

                public partial int Value { get; set; }

                public ObservableList<string> GetLogs() => BaseModel.LogEntries;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleReferences_OneDerived_OnlyDerivedMarkedWithDiagnostic()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class OtherModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(OtherModel otherModel, {|{{DiagnosticDescriptors.DerivedModelReferenceError.Id}}:DerivedModel|} derivedModel);

                public partial int Value { get; set; }

                public string GetInfo() => OtherModel.Name;
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleReferences_OneDerived_CodeFixRemovesOnlyDerivedAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class OtherModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(OtherModel otherModel, {|{{DiagnosticDescriptors.DerivedModelReferenceError.Id}}:DerivedModel|} derivedModel);

                public partial int Value { get; set; }

                public string GetInfo() => OtherModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public partial ObservableList<string> LogEntries { get; init; } = new();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : BaseModel
            {
                public partial int Counter { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class OtherModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel(OtherModel otherModel);

                public partial int Value { get; set; }

                public string GetInfo() => OtherModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task DeepInheritance_ReferenceDerivedModel_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class BaseModel : ObservableModel
            {
                public partial int BaseValue { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public abstract partial class MiddleModel : BaseModel
            {
                public partial int MiddleValue { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class DerivedModel : MiddleModel
            {
                public partial int DerivedValue { get; set; }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class ParentModel : ObservableModel
            {
                public partial ParentModel({|{{DiagnosticDescriptors.DerivedModelReferenceError.Id}}:DerivedModel|} derivedModel);

                public partial int Value { get; set; }
            }
        }
        """;
        await CodeFixVerifier.VerifyAnalyzerAsync(test);
    }
}
