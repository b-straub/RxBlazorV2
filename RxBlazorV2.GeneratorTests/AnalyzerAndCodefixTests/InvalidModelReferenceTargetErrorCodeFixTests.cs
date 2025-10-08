using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
        RxBlazorV2CodeFix.CodeFix.InvalidModelReferenceCodeFix>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

public class InvalidModelReferenceTargetErrorCodeFixTests
{
    [Fact]
    public async Task RemoveModelReference_RemovesInvalidReference()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            // This doesn't inherit from ObservableModel so should trigger the diagnostic
            public class UnregisteredModel
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<UnregisteredModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveModelReference_FromAttributeList()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            // This doesn't inherit from ObservableModel so should trigger the diagnostic
            public class UnregisteredService
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<UnregisteredService>|}, ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

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
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveInvalidInterfaceReferenceAsync()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IService
            {
                void Execute();
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<IService>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IService
            {
                void Execute();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveInvalidAbstractClassReferenceAsync()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract class BaseService
            {
                public abstract void Execute();
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<BaseService>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public abstract class BaseService
            {
                public abstract void Execute();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveInvalidNestedClassReferenceAsync()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class OuterClass
            {
                public class NestedService
                {
                    public string Value { get; set; }
                }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<OuterClass.NestedService>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class OuterClass
            {
                public class NestedService
                {
                    public string Value { get; set; }
                }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveInvalidGenericTypeReference_ClosedGeneric()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Collections.Generic;

        namespace Test
        {
            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<List<string>>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Collections.Generic;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveInvalidReference_FirstOfMultipleSeparateAttributeLists()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class Service1
            {
                public string Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ValidModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference<Service1>|}]
            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference<ValidModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class Service1
            {
                public string Value { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ValidModel : ObservableModel
            {
                public partial int Counter { get; set; }
            }

            [{|{{DiagnosticDescriptors.UnusedModelReferenceError.Id}}:ObservableModelReference<ValidModel>|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task RemoveInvalidReference_WithTypeofSyntax()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class UnregisteredModel
            {
                public string Name { get; set; }
            }

            [{|{{DiagnosticDescriptors.InvalidModelReferenceTargetError.Id}}:ObservableModelReference(typeof(UnregisteredModel))|}]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        // lang=csharp
        var fixedCode = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class UnregisteredModel
            {
                public string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class TestModel : ObservableModel
            {
                public partial int Value { get; set; }
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

}
