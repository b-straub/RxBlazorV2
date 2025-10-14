using MudBlazor;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ReactiveComposition;

[ObservableModelScope(ModelScope.Scoped)]
public partial class ModelGood : ObservableModel
{
    [ObservableTrigger(nameof(ShowError), nameof(CanSendError))]
    public partial string ErrorMessage { get; set; } = string.Empty;
    
    
    [ObservableCommand(nameof(ShowError), nameof(CanSendError))]
    public partial IObservableCommand<string> ShowErrorMessage{ get; }

    private void ShowError()
    {
        Snackbar.Add(ErrorMessage);
    }
    
    private void ShowError(string errorMessage)
    {
        Snackbar.Add(errorMessage);
    }
    
    private bool CanSendError()
    {
        return ErrorMessage.Length > 3;
    }
    
    public partial ModelGood(ISnackbar snackbar);
}