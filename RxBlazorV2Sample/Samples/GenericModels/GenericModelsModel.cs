using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.GenericModels;

[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModelsBaseModel<T, TP> : ObservableModel where T : class where TP : struct
{
    public required partial ObservableList<T> Items { get; init; }
    public required partial ObservableList<TP> Values { get; init; }
}

[ObservableModelReference(typeof(GenericModelsBaseModel<,>))]
[ObservableModelScope(ModelScope.Scoped)]
public partial class GenericModelsModel<T, TP> : ObservableModel where T : class where TP : struct
{
    public string Usage => "Generic models support type parameters with constraints for flexible, reusable patterns";
    public required partial ObservableList<LogEntry> LogEntries { get; init; }
    public partial string Status { get; set; } = "Ready";

    public IList<T> Items => GenericModelsBaseModel.Items;
    public IList<TP> Values => GenericModelsBaseModel.Values;

    [ObservableCommand(nameof(AddItem), nameof(CanAddItem))]
    public partial IObservableCommand<T> AddItemCommand { get; }

    [ObservableCommand(nameof(AddValue), nameof(CanAddValue))]
    public partial IObservableCommand<TP> AddValueCommand { get; }

    [ObservableCommand(nameof(ClearItems), nameof(CanClearItems))]
    public partial IObservableCommand ClearItemsCommand { get; }

    [ObservableCommand(nameof(ClearValues), nameof(CanClearValues))]
    public partial IObservableCommand ClearValuesCommand { get; }

    private void AddItem(T item)
    {
        GenericModelsBaseModel.Items.Add(item);
        Status = $"Added item. Total items: {GenericModelsBaseModel.Items.Count}";
        LogEntries.Add(new LogEntry($"Added item: {item}. Total: {GenericModelsBaseModel.Items.Count}", DateTime.Now));
    }

    private bool CanAddItem()
    {
        return GenericModelsBaseModel.Items.Count < 5;
    }

    private void AddValue(TP value)
    {
        GenericModelsBaseModel.Values.Add(value);
        Status = $"Added value. Total values: {GenericModelsBaseModel.Values.Count}";
        LogEntries.Add(new LogEntry($"Added value: {value}. Total: {GenericModelsBaseModel.Values.Count}", DateTime.Now));
    }

    private bool CanAddValue()
    {
        return GenericModelsBaseModel.Values.Count < 10;
    }

    private void ClearItems()
    {
        GenericModelsBaseModel.Items.Clear();
        Status = "All items cleared";
        LogEntries.Add(new LogEntry("All items cleared", DateTime.Now));
    }

    private bool CanClearItems()
    {
        return GenericModelsBaseModel.Items.Count > 0;
    }

    private void ClearValues()
    {
        GenericModelsBaseModel.Values.Clear();
        Status = "All values cleared";
        LogEntries.Add(new LogEntry("All values cleared", DateTime.Now));
    }

    private bool CanClearValues()
    {
        return GenericModelsBaseModel.Values.Count > 0;
    }
}
