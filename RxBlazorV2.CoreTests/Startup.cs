using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2Sample;
using RxBlazorV2Sample.Samples.BasicCommands;
using RxBlazorV2Sample.Samples.CommandsWithCanExecute;
using RxBlazorV2Sample.Samples.CommandsWithCancellation;
using RxBlazorV2Sample.Samples.CommandTriggers;
using RxBlazorV2Sample.Samples.CrossComponentCommunication;
using RxBlazorV2Sample.Samples.ObservableBatches;
using RxBlazorV2Sample.Samples.ParameterizedCommands;
using RxBlazorV2Sample.Samples.ValueEquality;

namespace RxBlazorV2.CoreTests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register all ObservableModels from the Sample project
        ObservableModels.Initialize(services);
        RxBlazorV2Sample.ObservableModels.Initialize(services);
    }
}
