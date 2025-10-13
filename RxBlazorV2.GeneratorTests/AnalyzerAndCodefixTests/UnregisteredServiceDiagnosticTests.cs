using RxBlazorV2Generator.Diagnostics;
using AnalyzerVerifier = RxBlazorV2.GeneratorTests.Helpers.CSharpAnalyzerVerifier<RxBlazorV2Generator.Analyzers.RxBlazorDiagnosticAnalyzer>;

namespace RxBlazorV2.GeneratorTests.AnalyzerAndCodefixTests;

/// <summary>
/// Tests for RXBG020: Partial constructor parameter type may not be registered in DI
/// </summary>
public class UnregisteredServiceDiagnosticTests
{
    [Fact]
    public async Task EmptyCode_NoErrorsExpected()
    {
        var test = @"";
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RegisteredObservableModel_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ConsumerModel : ObservableModel
            {
                // ObservableModel types are always recognized as registered
                public partial ConsumerModel(ServiceModel serviceModel);

                public string GetName() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task UnregisteredServiceInterface_DiagnosticExpected()
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
                // IValidationService is not detected as registered - should report info diagnostic
                public partial MyModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:IValidationService|} validationService);

                public bool CheckValid() => ValidationService.IsValid();
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task UnregisteredConcreteService_DiagnosticExpected()
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
                // EmailService is not detected as registered - should report info diagnostic
                public partial NotificationModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:EmailService|} emailService);

                public void Notify() => EmailService.SendEmail("test@example.com");
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleDependencies_SomeUnregistered_DiagnosticsOnUnregisteredOnly()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface ILogger { }
            public interface IValidator { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class ServiceModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class AppModel : ObservableModel
            {
                // Mixed: ServiceModel is ObservableModel (OK), ILogger and IValidator are unregistered (diagnostic)
                public partial AppModel(
                    ServiceModel serviceModel,
                    {|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:ILogger|} logger,
                    {|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:IValidator|} validator);

                public string GetInfo() => ServiceModel.Name;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleObservableModels_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class CounterModel : ObservableModel
            {
                public partial int Count { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsEnabled { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class DashboardModel : ObservableModel
            {
                // All ObservableModel types - no warnings
                public partial DashboardModel(CounterModel counterModel, SettingsModel settingsModel);

                public int GetCount() => CounterModel.Count;
                public bool IsEnabled() => SettingsModel.IsEnabled;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task UnregisteredService_TransientModel_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface ITemporaryService
            {
                void DoWork();
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class TemporaryModel : ObservableModel
            {
                // ITemporaryService is not detected as registered
                public partial TemporaryModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:ITemporaryService|} temporaryService);

                public void Execute() => TemporaryService.DoWork();
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task UnregisteredService_ScopedModel_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IUserContext
            {
                string GetCurrentUser();
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class UserModel : ObservableModel
            {
                // IUserContext is not detected as registered
                public partial UserModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:IUserContext|} userContext);

                public string CurrentUser => UserContext.GetCurrentUser();
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MixedModelAndServiceDependencies_DiagnosticsOnServicesOnly()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public interface IConfigService { }
            public interface ILoggerService { }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class SettingsModel : ObservableModel
            {
                public partial bool IsDebug { get; set; }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class AppModel : ObservableModel
            {
                // ObservableModel OK, services not detected
                public partial AppModel(
                    SettingsModel settingsModel,
                    {|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:IConfigService|} configService,
                    {|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:ILoggerService|} loggerService);

                public bool IsDebugMode => SettingsModel.IsDebug;
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
