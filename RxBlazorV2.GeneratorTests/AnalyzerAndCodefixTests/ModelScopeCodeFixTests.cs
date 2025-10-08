using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class ModelScopeCodeFixTests
{
    [Fact]
    public async Task ChangeScopeToSingleton_FixesExistingScopedAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Component;

        namespace Test
        {
            [{|{{DiagnosticDescriptors.SharedModelNotSingletonError.Id}}:ObservableModelScope(ModelScope.Scoped)|}]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class TestComponent1 : ObservableComponent<TestModel>
            {
            }

            public partial class TestComponent2 : ObservableComponent<TestModel>
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RemoveScopeAttribute_RemovesExistingTransientAttribute()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Component;

        namespace Test
        {
            [{|{{DiagnosticDescriptors.SharedModelNotSingletonError.Id}}:ObservableModelScope(ModelScope.Transient)|}]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            public partial class TestComponent1 : ObservableComponent<TestModel>
            {
            }

            public partial class TestComponent2 : ObservableComponent<TestModel>
            {
            }
        }
        """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

}
