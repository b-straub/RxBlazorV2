using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

public enum DerivedPhase
{
    Unknown,
    Waiting,
    Ready
}

/// <summary>
/// Page model whose own property is derived from a referenced model by an auto-detected
/// internal observer. There is intentionally no initial pull in OnContextReady: the
/// observer is wired in the generated constructor, so it already runs before the
/// component has subscribed to <see cref="ObservableModel.Observable"/>.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Transient)]
public partial class DerivedPhaseModel : ObservableModel
{
    public partial DerivedPhaseModel(BootStateModel bootState);

    public partial DerivedPhase Phase { get; set; } = DerivedPhase.Unknown;

    private void OnBootStateChanged()
    {
        Phase = BootState.State == BootPhase.Ready ? DerivedPhase.Ready : DerivedPhase.Waiting;
    }
}
