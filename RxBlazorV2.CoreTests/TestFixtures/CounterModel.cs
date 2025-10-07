using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Transient)]
public partial class CounterModel : ObservableModel
{
    public partial int Counter1 { get; set; }
    public partial int Counter2 { get; set; }
    public partial int Counter3 { get; set; }
}
