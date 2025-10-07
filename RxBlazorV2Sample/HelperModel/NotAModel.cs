using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace RxBlazorV2Sample.HelperModel;

// This class exists but does NOT implement IObservableModel
// This should trigger RXBG007 without causing a compiler error
public partial class NotAModel
{
    public string Name { get; set; } = "Not a model";
}