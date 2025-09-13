using RxBlazorV2Generator.Diagnostics;

using CodeFixVerifier =
    RxBlazorV2Test.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2Test.Tests;

public class MissingDIRegistrationCodeFixTests
{
    [Fact]
    public async Task RemoveModelReference_RemovesInvalidReference()
    {
        // lang=csharp
        var test = @$"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{{
    // This doesn't inherit from ObservableModel so should trigger the diagnostic
    public class UnregisteredModel
    {{
        public string Name {{ get; set; }}
    }}

    [{{|{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}:ObservableModelReference<UnregisteredModel>|}}]
    [ObservableModelScope(ModelScope.Scoped)]
    public partial class TestModel : ObservableModel
    {{
        public partial int Value {{ get; set; }}
    }}
}}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    // This doesn't inherit from ObservableModel so should trigger the diagnostic
    public class UnregisteredModel
    {
        public string Name { get; set; }
    }

    [ObservableModelScope(ModelScope.Scoped)]
    public partial class TestModel : ObservableModel
    {
        public partial int Value { get; set; }
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

    [Fact]
    public async Task RemoveModelReference_FromAttributeList()
    {
        // lang=csharp
        var test = @$"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{{
    // This doesn't inherit from ObservableModel so should trigger the diagnostic
    public class UnregisteredService
    {{
        public string Name {{ get; set; }}
    }}

    [{{|{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}:ObservableModelReference<UnregisteredService>|}}, ObservableModelScope(ModelScope.Scoped)]
    public partial class TestModel : ObservableModel
    {{
        public partial int Value {{ get; set; }}
    }}
}}";

        // lang=csharp
        var fixedCode = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    // This doesn't inherit from ObservableModel so should trigger the diagnostic
    public class UnregisteredService
    {
        public string Name { get; set; }
    }

    [ObservableModelScope(ModelScope.Scoped)]
    public partial class TestModel : ObservableModel
    {
        public partial int Value { get; set; }
    }
}";
        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode, codeActionIndex: 0);
    }

}