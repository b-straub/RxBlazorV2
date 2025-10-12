using RxBlazorV2.Component;

namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class TestCodeBehindAccessComponent : ObservableComponent<TestCodeBehindAccessModel>
{
    // Expression-bodied property accessing Model.PropertyB
    private string AccessedViaExpressionProperty => Model.PropertyB;

    // Regular property with getter accessing Model.PropertyC
    private string AccessedViaRegularProperty
    {
        get
        {
            return Model.PropertyC;
        }
    }

    // Method accessing Model.Counter
    private int GetPropertyFromMethod()
    {
        return Model.Counter;
    }

    // Method with conditional logic accessing Model.IsActive
    private bool CheckActiveStatus()
    {
        if (Model.IsActive)
        {
            return true;
        }
        return false;
    }
}
