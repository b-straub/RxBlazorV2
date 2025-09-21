using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.HelperModel;

[ObservableModelReference<NotAModel>]
[ObservableModelReference<Models.CounterModel>]
[ObservableModelScope(ModelScope.Transient)]
public partial class Switcher : ObservableModel
{
    public partial bool AddMode { get; set; }
    
    public partial bool Add10 { get; set; }
    
    public int Count => AddMode ? 
        Add10 ? 10 : 5 
        : 
        Add10 ? -10 : -5;
    
    [ObservableCommand(nameof(AddTenCMD), nameof(CanAddTen))]
    public partial IObservableCommand SwitchOnAdd10 { get; }
    
    private void AddTenCMD()
    {
        Add10 = true;
    }
    
    private bool CanAddTen()
    {
        return AddMode && !Add10 && CounterModel.Counter2 < 20;
    }
}