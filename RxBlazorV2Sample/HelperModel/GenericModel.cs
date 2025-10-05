using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Interfaces;

namespace RxBlazorV2Sample.HelperModel;

[ObservableModelReference<ISettingsModel>]
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T, P> : ObservableModel where T : class where P : struct
{
    public partial ObservableList<T> Tlist { get; set; }
    public partial ObservableList<P> Plist { get; set; }
    
    protected override void OnContextReady()
    {
        Console.WriteLine("OnContextReady");
    }
}

[ObservableModelReference(typeof(GenericModel<,>))]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AnotherGenericModel<T, P> : ObservableModel where T : class where P : struct
{
    public IList<T> Tlist => GenericModel.Tlist;
    public IList<P> Plist => GenericModel.Plist;
    
    [ObservableCommand(nameof(AddItemToTList), nameof(AddItemToTListCe))]
    public partial IObservableCommand<T> AddToTList { get; }
    
    [ObservableCommand(nameof(ClearTListCmd), nameof(ClearTListCmdCe))]
    public partial IObservableCommand ClearTList { get; }
    
    [ObservableCommand(nameof(AddItemToPList), nameof(AddItemToPListCe))]
    public partial IObservableCommand<P> AddToPList { get; }
    
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
    
    private void AddItemToPList(P item)
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
public partial class GenericModelTwoParams<T, U> : ObservableModel where T : class where U : struct
{
    public partial T Value { get; set; } = null!;
    public partial U SecondValue { get; set; }
}

[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference(typeof(GenericModelTwoParams<,>))]
public partial class TestModel<T, U> : ObservableModel where T : class where U : struct
{
    public partial string Name { get; set; } = "";
}