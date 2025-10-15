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

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class GenericModelsModel<T, TP> : ObservableModel where T : class where TP : struct
{
    public string Usage => "Generic models support type parameters with constraints for flexible, reusable patterns";
    public required partial ObservableList<LogEntry> LogEntries { get; init; }
    public partial string Status { get; set; } = "Ready";

    // Declare partial constructor with generic base model dependency
    public partial GenericModelsModel(GenericModelsBaseModel<T, TP> genericModelsBase);

    public IList<T> Items => GenericModelsBase.Items;
    public IList<TP> Values => GenericModelsBase.Values;

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
        GenericModelsBase.Items.Add(item);
        Status = $"Added item. Total items: {GenericModelsBase.Items.Count}";
        LogEntries.Add(new LogEntry($"Added item: {item}. Total: {GenericModelsBase.Items.Count}", DateTime.Now));
    }

    private bool CanAddItem()
    {
        return GenericModelsBase.Items.Count < 5;
    }

    private void AddValue(TP value)
    {
        GenericModelsBase.Values.Add(value);
        Status = $"Added value. Total values: {GenericModelsBase.Values.Count}";
        LogEntries.Add(new LogEntry($"Added value: {value}. Total: {GenericModelsBase.Values.Count}", DateTime.Now));
    }

    private bool CanAddValue()
    {
        return GenericModelsBase.Values.Count < 10;
    }

    private void ClearItems()
    {
        GenericModelsBase.Items.Clear();
        Status = "All items cleared";
        LogEntries.Add(new LogEntry("All items cleared", DateTime.Now));
    }

    private bool CanClearItems()
    {
        return GenericModelsBase.Items.Count > 0;
    }

    private void ClearValues()
    {
        GenericModelsBase.Values.Clear();
        Status = "All values cleared";
        LogEntries.Add(new LogEntry("All values cleared", DateTime.Now));
    }

    private bool CanClearValues()
    {
        return GenericModelsBase.Values.Count > 0;
    }
}
