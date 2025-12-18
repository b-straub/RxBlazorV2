using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RxBlazorV2.MudBlazor.Sample;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
RxBlazorV2.MudBlazor.ObservableModels.Initialize(builder.Services);
ObservableModels.Initialize(builder.Services);

await builder.Build().RunAsync();
