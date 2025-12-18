using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorVSSampleComponents.ErrorManager;

[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorModel : ObservableModel, IErrorModel
{
    [ObservableComponentTrigger]
    public ObservableList<string> Errors { get; } = [];

    public void HandleError(Exception error)
    {
        Errors.Add(error.Message);
    }
}