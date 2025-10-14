using RxBlazorV2.Model;
using RxBlazorV2ExternalModel.Models;
using RxBlazorV2ExternalModel.TestService;
using System.Diagnostics.CodeAnalysis;

namespace RxBlazorV2Sample.Samples.ReactiveComposition;

public partial class ModelBad : ObservableModel
{
    public partial string ErrorMessage { get; set; } = string.Empty;

    [SuppressMessage("RxBlazorGenerator", "RXBG020:Partial constructor parameter type may not be registered in DI", Justification = "TestService registered externally")]
    public partial ModelBad(TestService model);
}