using RxBlazorV2.Model;

namespace RxBlazorV2Sample.HelperModel;

// This class exists but does NOT implement IObservableModel
// This should trigger RXBG007 without causing a compiler error
public partial class NotAModel
{
    public string Name { get; set; } = "Not a model";
}

[ObservableModelScope(ModelScope.Singleton)]
public partial class MyGenericModel<T> : ObservableModel where T : new()
{
    public partial T Value { get; set; } = new();
}

[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference(typeof(MyGenericModel<>))]
public partial class MyTestModel<T> : ObservableModel where T : new()
{
    public partial string Name { get; set; } = "";

    public T GetRefValue() => MyGenericModel.Value;
}