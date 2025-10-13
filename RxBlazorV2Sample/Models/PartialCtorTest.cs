using RxBlazorV2.Model;
using RxBlazorV2Sample.Interfaces;
using RxBlazorV2Sample.Services;

namespace RxBlazorV2Sample.Models;

[ObservableModelScope(ModelScope.Scoped)]
public partial class PartialCtorTest : ObservableModel
{
    public partial string SearchText { get; set; } = string.Empty;
    public partial PartialCtorTest(CounterModel counter, ISettingsModel settings, LocationService  locationService);

    public bool IsDay1 => Counter.Counter1 == 1 && Settings.IsDay;
}