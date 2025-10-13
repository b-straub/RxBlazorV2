using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ReactiveComposition;

public partial class ModelGood : ObservableModel
{
    public bool IsValid => ValidationService.IsValid();
    
    //[ObservableTrigger(nameof(UpdateValidation))]
    //[ObservableTrigger(nameof(UpdateValidationAsync), nameof(CanUpdateValidation))]
    //[ObservableTrigger(nameof(UpdateValidation))]
    public partial int Changer { get; set; }
    
    [ObservableCommand(nameof(UpdateValidation))]
    [ObservableCommandTrigger(nameof(Changer))]
    protected partial IObservableCommand ChangerCommand { get; }

    private void UpdateValidation()
    {
        ValidationService.SetCallerScope(true);
    }
    
    private async Task UpdateValidationAsync()
    {
        await Task.Delay(1000);
        ValidationService.SetCallerScope(true);
    }

    private bool CanUpdateValidation()
    {
        return Changer > 1;
    }
    
    public partial ModelGood(IValidationService validationService);
}