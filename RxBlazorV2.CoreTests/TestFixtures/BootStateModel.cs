using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

public enum BootPhase
{
    Unknown,
    Initializing,
    Ready
}

/// <summary>
/// Stands in for an app-wide state model (database / auth state) that is driven from
/// outside the page — here by <see cref="EarlyEmitHost"/> in its OnAfterRender.
/// </summary>
[ObservableModelScope(ModelScope.Singleton)]
public partial class BootStateModel : ObservableModel
{
    public partial BootPhase State { get; set; } = BootPhase.Unknown;
}
