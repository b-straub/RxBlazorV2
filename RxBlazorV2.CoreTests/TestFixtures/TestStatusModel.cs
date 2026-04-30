using ObservableCollections;
using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

/// <summary>
/// Concrete test status model. Provides the required override for <see cref="StatusBaseModel.Messages"/>.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestStatusModel : StatusBaseModel
{
    public override ObservableList<StatusMessage> Messages { get; } = [];
}
