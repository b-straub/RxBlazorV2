namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class AllPropertiesFilterTestComponent
{
    /// <summary>
    /// Manually specified filter for test - includes all properties used in razor markup.
    /// </summary>
    protected override string[] Filter()
    {
        return [
            "Model.Counter",
            "Model.Name",
            "Model.BatchProperty1",
            "Model.BatchProperty2"
        ];
    }
}
