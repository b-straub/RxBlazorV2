using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ReactivePatternSample;
using ReactivePatternSample.Storage.Services;

// Initialize all domain models - each domain has its own generated ObservableModels class
using ReactivePatternSampleStorageModels = ReactivePatternSample.Storage.ObservableModels;
using ReactivePatternSampleStatusModels = ReactivePatternSample.Status.ObservableModels;
using ReactivePatternSampleSettingsModels = ReactivePatternSample.Settings.ObservableModels;
using ReactivePatternSampleAuthModels = ReactivePatternSample.Auth.ObservableModels;
using ReactivePatternSampleTodoModels = ReactivePatternSample.Todo.ObservableModels;
using ReactivePatternSampleShareModels = ReactivePatternSample.Share.ObservableModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

// Add HttpClient for any future API calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register persistence services for StorageModel
// LocalStorageService provides browser localStorage access
// StoragePersistenceObserver uses [ObservableModelObserver] to auto-persist when model changes
builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<StoragePersistenceObserver>();

// Initialize all domain ObservableModels
// Order matters: dependencies must be registered before dependents
// Storage → Status → Settings → Auth → Todo → Share
ReactivePatternSampleStorageModels.Initialize(builder.Services);  // No dependencies (foundation)
ReactivePatternSampleStatusModels.Initialize(builder.Services);   // No domain dependencies
ReactivePatternSampleSettingsModels.Initialize(builder.Services); // Depends on Storage
ReactivePatternSampleAuthModels.Initialize(builder.Services);     // Depends on Storage, Status
ReactivePatternSampleTodoModels.Initialize(builder.Services);     // Depends on Storage, Auth, Status
ReactivePatternSampleShareModels.Initialize(builder.Services);    // Depends on Todo, Status, Settings

// Initialize the auto-generated RxBlazorV2 layout model (ensures all singletons are instantiated together)
builder.Services.InitializeLayout();
await builder.Build().RunAsync();
