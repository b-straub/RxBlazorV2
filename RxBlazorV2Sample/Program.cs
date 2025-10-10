using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RxBlazorV2Sample;
using RxBlazorV2Sample.Models;
using RxBlazorV2Sample.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
// Add weather services
builder.Services.AddScoped<WeatherCodeParser>();
builder.Services.AddScoped<OpenMeteoApiClient>();
builder.Services.AddScoped<LocationService>();

ObservableModels.Initialize(builder.Services);
RxBlazorV2ExternalModel.ObservableModels.Initialize(builder.Services);

// Register generic models for samples
ObservableModels.GenericModelsBaseModel<string, int>(builder.Services);
ObservableModels.GenericModelsModel<string, int>(builder.Services);

// Register generic models for legacy examples
ObservableModels.GenericModel<string, int>(builder.Services);
ObservableModels.AnotherGenericModel<string, int>(builder.Services);

await builder.Build().RunAsync();
