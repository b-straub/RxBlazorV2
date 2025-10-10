using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.HelperModel;

[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T, TP> : ObservableModel where T : class where TP : struct
{
    public required partial ObservableList<T> Tlist { get; init; }
    public required partial ObservableList<TP> Plist { get; init; }
    
    protected override void OnContextReady()
    {
        Console.WriteLine("OnContextReady");
    }
}

[ObservableModelReference(typeof(GenericModel<,>))]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AnotherGenericModel<T, TP> : ObservableModel where T : class where TP : struct
{
    public IList<T> Tlist => GenericModel.Tlist;
    public IList<TP> Plist => GenericModel.Plist;
    
    [ObservableCommand(nameof(AddItemToTList), nameof(AddItemToTListCe))]
    public partial IObservableCommand<T> AddToTList { get; }
    
    [ObservableCommand(nameof(ClearTListCmd), nameof(ClearTListCmdCe))]
    public partial IObservableCommand ClearTList { get; }
    
    [ObservableCommand(nameof(AddItemToPList), nameof(AddItemToPListCe))]
    public partial IObservableCommand<TP> AddToPList { get; }
    
    [ObservableCommand(nameof(ClearPListCmd), nameof(ClearPListCmdCe))]
    public partial IObservableCommand ClearPList { get; }
    
    private void AddItemToTList(T item)
    {
        Tlist.Add(item);
    }

    private bool AddItemToTListCe()
    {
        return Tlist.Count < 3;
    }
    
    private void ClearTListCmd()
    {
        Tlist.Clear();
    }
    
    private bool ClearTListCmdCe()
    {
        return Tlist.Count > 0;
    }
    
    private void AddItemToPList(TP item)
    {
        Plist.Add(item);
    }
    
    private bool AddItemToPListCe()
    {
        return Plist.Count < 5;
    }
    
    private void ClearPListCmd()
    {
        Plist.Clear();
    }
    
    private bool ClearPListCmdCe()
    {
        return Plist.Count > 0;
    }
    
    protected override void OnContextReady()
    {
        Console.WriteLine("OnContextReady");
    }
}

[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModelTwoParams<T, TU> : ObservableModel where T : class where TU : struct
{
    public partial T Value { get; set; } = null!;
    public partial TU SecondValue { get; set; }
}

[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference(typeof(GenericModelTwoParams<,>))]
public partial class TestModel<T, TU> : ObservableModel where T : class where TU : struct
{
    public partial string Name { get; set; } = "";

    public (T, TU) GetReferencedProperties()
    {
        return (GenericModelTwoParams.Value, GenericModelTwoParams.SecondValue);
    }
}