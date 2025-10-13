using Microsoft.AspNetCore.Components;
using RxBlazorV2.Component;
using RxBlazorV2Sample.Models;

namespace RxBlazorV2Sample.Components;

public partial class InjectedOnlyCb : ObservableComponent
{
    [Inject] 
    public required CounterModel CounterModel { get; init; }
    
    [Inject] 
    public required SettingsModel SettingsModel { get; init; }
}