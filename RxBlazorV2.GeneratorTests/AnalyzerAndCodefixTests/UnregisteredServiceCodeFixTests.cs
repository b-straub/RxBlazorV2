using RxBlazorV2Generator.Diagnostics;
using CodeFixVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpCodeFixVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer,
    RxBlazorV2CodeFix.CodeFix.UnregisteredServiceCodeFixProvider>;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG020 (unregistered service warning) code fix functionality.
/// RXBG020 is reported by the generator, and the test infrastructure includes the generator.
/// </summary>
public class UnregisteredServiceCodeFixTests
{
    [Fact]
    public async Task AddSuppressMessageAttribute_SingleParameter()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IValidationService
            {
                bool IsValid();
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MyModel : ObservableModel
            {
                public partial MyModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:IValidationService|} validationService);

                public bool CheckValid() => ValidationService.IsValid();
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Diagnostics.CodeAnalysis;

        namespace Test
        {
            public interface IValidationService
            {
                bool IsValid();
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MyModel : ObservableModel
            {
                [SuppressMessage("RxBlazorGenerator", "{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:Partial constructor parameter type may not be registered in DI", Justification = "IValidationService registered externally")]
                public partial MyModel(IValidationService validationService);

                public bool CheckValid() => ValidationService.IsValid();
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task AddSuppressMessageAttribute_WithObservableModelReference()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface ILogger { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class AppModel : ObservableModel
            {
                public partial AppModel(
                    ServiceModel serviceModel,
                    {|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:ILogger|} logger);

                public string GetInfo() => ServiceModel.Name;
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Diagnostics.CodeAnalysis;

        namespace Test
        {
            public interface ILogger { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class AppModel : ObservableModel
            {
                [SuppressMessage("RxBlazorGenerator", "{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:Partial constructor parameter type may not be registered in DI", Justification = "ILogger registered externally")]
                public partial AppModel(
                    ServiceModel serviceModel,
                    ILogger logger);

                public string GetInfo() => ServiceModel.Name;
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task AddSuppressMessageAttribute_WithConcreteClass()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public class EmailService
            {
                public void SendEmail(string to) { }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class NotificationModel : ObservableModel
            {
                public partial NotificationModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:EmailService|} emailService);

                public void Notify() => EmailService.SendEmail("test@example.com");
            }
        }
        """;

        // lang=csharp
        var fixedCode = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Diagnostics.CodeAnalysis;

        namespace Test
        {
            public class EmailService
            {
                public void SendEmail(string to) { }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class NotificationModel : ObservableModel
            {
                [SuppressMessage("RxBlazorGenerator", "{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:Partial constructor parameter type may not be registered in DI", Justification = "EmailService registered externally")]
                public partial NotificationModel(EmailService emailService);

                public void Notify() => EmailService.SendEmail("test@example.com");
            }
        }
        """;

        await CodeFixVerifier.VerifyCodeFixAsync(test, fixedCode);
    }
}
