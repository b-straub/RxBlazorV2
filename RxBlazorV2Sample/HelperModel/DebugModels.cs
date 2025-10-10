using RxBlazorV2.Model;

namespace RxBlazorV2Sample.HelperModel;

public abstract partial class BaseModel : ObservableModel
{
    public partial int BaseValue { get; set; }
}

public abstract partial class MiddleModel : BaseModel
{
    public partial int MiddleValue { get; set; }
}

[ObservableModelScope(ModelScope.Scoped)]
public partial class DerivedModel : MiddleModel
{
    public partial int DerivedValue { get; set; }
}

[ObservableModelScope(ModelScope.Transient)] // this should trigger DiagnosticDescriptors.DerivedModelReferenceError.Id
public partial class ParentModel : DerivedModel
{
    public partial int Value { get; set; }
    
    public int GetBaseInt() => BaseValue;
}