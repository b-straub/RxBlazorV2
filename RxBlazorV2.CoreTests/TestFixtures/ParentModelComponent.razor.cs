namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class ParentModelComponent
{
    /// <summary>
    /// Manually specified filter for test - includes referenced model properties.
    /// Uses format: "Model.CounterModel.PropertyName"
    /// Counter3 is intentionally NOT included (not used in component).
    /// </summary>
    protected override string[] Filter()
    {
        return [
            "Model.CounterModel.Counter1",
            "Model.CounterModel.Counter2"
        ];
    }
}
