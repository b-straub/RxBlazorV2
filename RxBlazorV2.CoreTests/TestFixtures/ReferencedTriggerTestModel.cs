using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Singleton)]
public partial class ReferencedTriggerTestModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string TriggerProperty { get; set; } = "Initial Referenced";
}
