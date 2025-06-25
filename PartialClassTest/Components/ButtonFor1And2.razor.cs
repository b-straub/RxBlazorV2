using RxBlazorV2.Component;
using RxBlazorV2Sample.Model;
using RxBlazorV2Sample.HelperModel;

namespace RxBlazorV2Sample.Components;

public partial class ButtonFor1And2 : ObservableComponent<CounterModel>
{
    private readonly Switcher _switcher;
    
    private string AddText => _switcher.AddMode ? 
        _switcher.Add10 ? "Add 10 to Counter 2" : "Add 5 to Counter 2" 
        : 
        _switcher.Add10 ? "Subtract 10 from Counter 2" : "Subtract 5 from Counter 2";
}