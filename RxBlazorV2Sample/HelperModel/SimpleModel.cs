using MudBlazor;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.HelperModel;

[ObservableModelScope(ModelScope.Scoped)]
public partial class SimpleModel : ObservableModel
{
    public partial int Test { get; set; }
    public partial SimpleModel(ISnackbar snackbar);
}