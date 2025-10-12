using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Singleton)]
public partial class TestExtraInjectedModel : ObservableModel
{
    public partial string InjectedPropertyA { get; set; } = "Extra Injected A";
}
