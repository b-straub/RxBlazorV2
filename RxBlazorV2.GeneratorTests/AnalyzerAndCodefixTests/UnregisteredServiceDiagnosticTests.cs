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

    [Fact]
    public async Task RegisteredInterfaceService_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.Extensions.DependencyInjection;

        namespace Test
        {
            public interface IValidationService
            {
                bool IsValid();
            }

            public class ValidationService : IValidationService
            {
                public bool IsValid() => true;
            }

            public static class ServiceRegistration
            {
                public static void ConfigureServices(IServiceCollection services)
                {
                    // Register the service
                    services.AddSingleton<IValidationService, ValidationService>();
                }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MyModel : ObservableModel
            {
                // IValidationService is registered - no diagnostic expected
                public partial MyModel(IValidationService validationService);

                public bool CheckValid() => ValidationService.IsValid();
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RegisteredConcreteService_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.Extensions.DependencyInjection;

        namespace Test
        {
            public class EmailService
            {
                public void SendEmail(string to) { }
            }

            public static class ServiceRegistration
            {
                public static void ConfigureServices(IServiceCollection services)
                {
                    // Register the concrete service
                    services.AddScoped<EmailService>();
                }
            }

            [ObservableModelScope(ModelScope.Scoped)]
            public partial class NotificationModel : ObservableModel
            {
                // EmailService is registered - no diagnostic expected
                public partial NotificationModel(EmailService emailService);

                public void Notify() => EmailService.SendEmail("test@example.com");
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RegisteredAndUnregisteredServices_DiagnosticOnUnregisteredOnly()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.Extensions.DependencyInjection;

        namespace Test
        {
            public interface IRegisteredService { }
            public class RegisteredService : IRegisteredService { }
            public interface IUnregisteredService { }

            public static class ServiceRegistration
            {
                public static void ConfigureServices(IServiceCollection services)
                {
                    // Only register IRegisteredService
                    services.AddSingleton<IRegisteredService, RegisteredService>();
                }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MyModel : ObservableModel
            {
                // IRegisteredService is registered (OK), IUnregisteredService is not (diagnostic)
                public partial MyModel(
                    IRegisteredService registeredService,
                    {|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:IUnregisteredService|} unregisteredService);
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ServiceRegisteredViaFactory_DiagnosticExpected()
    {
        // lang=csharp
        var test = $$"""

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.Extensions.DependencyInjection;

        namespace Test
        {
            public class DatabaseService
            {
                public DatabaseService(string connectionString) { }
            }

            public static class ServiceRegistration
            {
                public static void ConfigureServices(IServiceCollection services)
                {
                    // Register via factory - may not be detected by static analysis
                    services.AddSingleton(sp => new DatabaseService("connection-string"));
                }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class DataModel : ObservableModel
            {
                // Factory detection is complex and may not work in all cases
                // This diagnostic can be safely ignored if the service is actually registered
                public partial DataModel({|{{DiagnosticDescriptors.UnregisteredServiceWarning.Id}}:DatabaseService|} databaseService);
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleServicesAllRegistered_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.Extensions.DependencyInjection;

        namespace Test
        {
            public interface IServiceA { }
            public class ServiceA : IServiceA { }
            public interface IServiceB { }
            public class ServiceB : IServiceB { }
            public interface IServiceC { }
            public class ServiceC : IServiceC { }

            public static class ServiceRegistration
            {
                public static void ConfigureServices(IServiceCollection services)
                {
                    // All services registered as Singleton to avoid scope violations
                    services.AddSingleton<IServiceA, ServiceA>();
                    services.AddSingleton<IServiceB, ServiceB>();
                    services.AddSingleton<IServiceC, ServiceC>();
                }
            }

            [ObservableModelScope(ModelScope.Singleton)]
            public partial class MyModel : ObservableModel
            {
                // All services are registered - no diagnostics expected
                public partial MyModel(
                    IServiceA serviceA,
                    IServiceB serviceB,
                    IServiceC serviceC);
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RegisteredServiceDifferentScopes_NoErrorsExpected()
    {
        // lang=csharp
        var test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using Microsoft.Extensions.DependencyInjection;

        namespace Test
        {
            public interface ISingletonService { }
            public class SingletonService : ISingletonService { }
            public interface IScopedService { }
            public class ScopedService : IScopedService { }
            public interface ITransientService { }
            public class TransientService : ITransientService { }

            public static class ServiceRegistration
            {
                public static void ConfigureServices(IServiceCollection services)
                {
                    services.AddSingleton<ISingletonService, SingletonService>();
                    services.AddScoped<IScopedService, ScopedService>();
                    services.AddTransient<ITransientService, TransientService>();
                }
            }

            [ObservableModelScope(ModelScope.Transient)]
            public partial class MyModel : ObservableModel
            {
                // Transient model can inject all service scopes - no diagnostics expected
                public partial MyModel(
                    ISingletonService singletonService,
                    IScopedService scopedService,
                    ITransientService transientService);
            }
        }
        """;
        await AnalyzerVerifier.VerifyAnalyzerAsync(test);
    }
}
