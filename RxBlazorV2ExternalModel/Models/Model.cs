using ObservableCollections;
using RxBlazorV2.Attributes;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2ExternalModel.Type;

namespace RxBlazorV2ExternalModel.Models;

[ObservableModelScope(ModelScope.Singleton)]
[ObservableComponent]
public partial class TestModel : ObservableModel
{
    public const string CommonBatch = "common";
    public const string ListBatch = "list";

    [ObservableComponentTrigger] // generates OnNotInBatchChanged only
    [ObservableComponentTriggerAsync] // generates OnNotInBatchChangedAsync only
    public partial int NotInBatch { get; set; }
    
    [ObservableBatch(CommonBatch)]
    public partial int InBatch { get; set; }

    [ObservableComponentTrigger]
    [ObservableBatch(ListBatch)]
    [ObservableBatch(CommonBatch)]
    public partial ObservableList<ListType> TestList { get; set; }
    
    [ObservableCommand(nameof(AddItemToTListAsync), nameof(AddItemToTListCe))]
    public partial IObservableCommandAsync<ListType> AddToTList { get; }
    
    [ObservableCommand(nameof(ClearListCmd), nameof(ClearListCmdCe))]
    public partial IObservableCommand ClearList { get; }
    
    private async Task AddItemToTListAsync(ListType item, CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        TestList.Add(item);
    }

    private bool AddItemToTListCe()
    {
        return TestList.Count < 3;
    }
    
    private void ClearListCmd()
    {
        TestList.Clear();
    }
    
    private bool ClearListCmdCe()
    {
        return TestList.Count > 0;
    }
}