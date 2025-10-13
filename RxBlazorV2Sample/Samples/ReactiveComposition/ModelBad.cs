using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ReactiveComposition;

public partial class ModelBad : ObservableModel
{
    public bool IsValid => ValidationService.IsValid();

    public partial ModelBad(IValidationService validationService);
}