using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RxBlazorV2.MudBlazor.Sample;
using RxBlazorV2.MudBlazor.Sample.Model;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
ObservableModels.Initialize(builder.Services);

await builder.Build().RunAsync();
