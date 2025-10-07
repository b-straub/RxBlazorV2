using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Transient)]
[ObservableModelReference<CounterModel>]
public partial class ParentModel : ObservableModel
{
    public partial bool AddMode { get; set; }
    public partial bool Add10 { get; set; }

    // Expose CounterModel for testing (SG generates protected property)
    public CounterModel GetCounterModel() => CounterModel;

    // This property uses Counter1 and Counter2 from CounterModel
    // The SG should detect this and create a filtered subscription
    public bool IsValid =>
        AddMode && !Add10 && CounterModel is { Counter2: < 20, Counter1: < 10 };

    // Another property that uses Counter1 only
    public bool IsCounter1Low => CounterModel.Counter1 < 5;

    // Property that uses Counter2 only
    public bool IsCounter2High => CounterModel.Counter2 > 15;

    // Property that uses both Counter1 and Counter2
    public int TotalCount => CounterModel.Counter1 + CounterModel.Counter2;
}
