using RxBlazorV2.Model;

namespace RxBlazorV2Sample.HelperModel;

// This class exists but does NOT implement IObservableModel
// This should trigger RXBG007 without causing a compiler error
public partial class NotAModel : ObservableModel
{
    public string Name { get; set; } = "Not a model";
}