using RxBlazorV2.Model;

namespace RxBlazorVSSampleComponents.ErrorManager;

[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Message { get; set; } = string.Empty;
}