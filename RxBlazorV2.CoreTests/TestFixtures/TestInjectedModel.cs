using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Singleton)]
public partial class TestInjectedModel : ObservableModel
{
    public partial string InjectedPropertyA { get; set; } = "Injected A";
    public partial string InjectedPropertyB { get; set; } = "Injected B";
    public partial int InjectedCounter { get; set; } = 0;
}
