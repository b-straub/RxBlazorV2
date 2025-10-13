using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ReactiveComposition;

public partial class ModelGood : ObservableModel
{
    public bool IsValid => ValidationService.IsValid();

    public partial ModelGood(IValidationService validationService);
}