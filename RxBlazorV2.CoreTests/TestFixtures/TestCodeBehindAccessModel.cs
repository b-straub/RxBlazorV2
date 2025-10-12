using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Transient)]
public partial class TestCodeBehindAccessModel : ObservableModel
{
    public partial string PropertyA { get; set; } = string.Empty;
    public partial string PropertyB { get; set; } = string.Empty;
    public partial string PropertyC { get; set; } = string.Empty;
    public partial int Counter { get; set; }
    public partial bool IsActive { get; set; }
}
