using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RxBlazorV2Sample;
using RxBlazorV2Sample.Model;
using RxBlazorV2Sample.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddObservableModels();

// Add weather services
builder.Services.AddScoped<WeatherCodeParser>();
builder.Services.AddScoped<OpenMeteoApiClient>();
builder.Services.AddScoped<LocationService>();

await builder.Build().RunAsync();
