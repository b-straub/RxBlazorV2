using MudBlazor;
using RxBlazorV2.Model;

namespace RxBlazorVSSampleComponents.ErrorManager;

[ObservableComponent]

[ObservableModelScope(ModelScope.Scoped)]
public partial class ErrorModel : ObservableModel
{
    public partial string Message { get; set; } = string.Empty;
    
    public partial string Message1 { get; set; } = string.Empty;
    
    public partial ErrorModel(ISnackbar  snackbar);
}