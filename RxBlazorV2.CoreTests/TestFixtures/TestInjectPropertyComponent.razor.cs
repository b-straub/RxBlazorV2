using Microsoft.AspNetCore.Components;
using RxBlazorV2.Component;

namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class TestInjectPropertyComponent : ObservableComponent<TestInjectedModel>
{
    [Inject]
    public required TestExtraInjectedModel ExtraModel { get; init; }
}
