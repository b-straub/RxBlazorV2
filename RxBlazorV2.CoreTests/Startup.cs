using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2Sample.Samples.CallbackTriggers;

namespace RxBlazorV2.CoreTests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register all ObservableModels from the Sample project
        ObservableModels.Initialize(services);
        RxBlazorV2Sample.ObservableModels.Initialize(services);
        services.AddScoped<CallbackTriggersService>();
    }
}
