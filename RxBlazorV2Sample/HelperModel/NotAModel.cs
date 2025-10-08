using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace RxBlazorV2Sample.HelperModel;

// This class exists but does NOT implement IObservableModel
// This should trigger RXBG007 without causing a compiler error
public partial class NotAModel
{
    public string Name { get; set; } = "Not a model";
}

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel
    {
        public partial T Value { get; set; }
    }

    [ObservableModelScope(ModelScope.Singleton)]
    [ObservableModelReference(typeof(GenericModel<>))]
    public partial class TestModel<T> : ObservableModel
    {
        public partial string Name { get; set; } = "";

        public T GetProp()
        {
            return GenericModel.Value;
        }
    }
}