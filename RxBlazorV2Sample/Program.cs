using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RxBlazorV2Sample;
using RxBlazorV2Sample.Samples.ModelObservers;
using RxBlazorV2Sample.Samples.ServiceModelInteraction;
using RxBlazorV2Sample.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Add HttpClient for external API calls
builder.Services.AddScoped(_ => new HttpClient());

// Add weather services
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<OpenMeteoApiClient>();

ObservableModels.Initialize(builder.Services);
RxBlazorV2ExternalModel.ObservableModels.Initialize(builder.Services);
RxBlazorVSSampleComponents.ObservableModels.Initialize(builder.Services);

// Register generic models for samples
ObservableModels.GenericModelsBaseModel<string, int>(builder.Services);
ObservableModels.GenericModelsModel<string, int>(builder.Services);

// Register model observers sample service
builder.Services.AddScoped<ModelObserversService>();

// Register service-model interaction sample service
builder.Services.AddScoped<ProcessingService>();

await builder.Build().RunAsync();
