using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2ExternalModel.Type;

namespace RxBlazorV2ExternalModel.Models;

public partial class TestModel : ObservableModel
{
    public partial ObservableList<ListType> TestList { get; set; }
    
    [ObservableCommand(nameof(AddItemToTList), nameof(AddItemToTListCe))]
    public partial IObservableCommand<ListType> AddToTList { get; }
    
    private void AddItemToTList(ListType item)
    {
        TestList.Add(item);
    }

    private bool AddItemToTListCe()
    {
        return TestList.Count < 3;
    }
}