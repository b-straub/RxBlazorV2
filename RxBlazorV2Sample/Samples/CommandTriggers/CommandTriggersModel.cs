using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.CommandTriggers;

public class SearchResult
{
    public string Query { get; set; } = "";
    public string Mode { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Cancelled { get; set; }
}

[ObservableModelScope(ModelScope.Scoped)]
public partial class CommandTriggersModel : SampleBaseModel
{
    public override string Usage => "Commands can automatically execute when specific properties change with different cancellation strategies";
    public partial string SwitchSearchText { get; set; } = "";
    public partial string MergeSearchText { get; set; } = "";
    public partial int MinLength { get; set; } = 3;
    public partial string SearchResults { get; set; } = "No searches performed yet";
    public partial int SearchCount { get; set; }

    public partial ObservableList<SearchResult> SwitchSearchResults { get; set; } = [];
    public partial ObservableList<SearchResult> MergeSearchResults { get; set; } = [];

    // Cancelable command - uses AwaitOperation.Switch (only latest execution runs)
    [ObservableCommand(nameof(PerformSwitchSearchAsync), nameof(CanPerformSwitchSearch))]
    [ObservableCommandTrigger(nameof(SwitchSearchText))]
    public partial IObservableCommandAsync SwitchSearchCommand { get; }

    // Non-cancelable command - runs in parallel (all executions complete)
    [ObservableCommand(nameof(PerformMergeSearchAsync), nameof(CanPerformMergeSearch))]
    [ObservableCommandTrigger(nameof(MergeSearchText))]
    public partial IObservableCommandAsync MergeSearchCommand { get; }

    [ObservableCommand(nameof(ClearResults), nameof(CanClearResults))]
    public partial IObservableCommand ClearResultsCommand { get; }

    private async Task PerformSwitchSearchAsync(CancellationToken ct)
    {
        var searchQuery = SwitchSearchText;
        var result = new SearchResult
        {
            Query = searchQuery,
            Mode = "Switch",
            StartTime = DateTime.Now
        };

        SwitchSearchResults.Add(result);
        LogEntries.Add(new LogEntry($"Switch search started for '{searchQuery}'", DateTime.Now));

        try
        {
            await Task.Delay(800, ct);
            result.EndTime = DateTime.Now;
            SearchCount++;
            SearchResults = $"Latest Switch search completed for '{searchQuery}' at {DateTime.Now:HH:mm:ss}";
            LogEntries.Add(new LogEntry($"Switch search #{SearchCount} completed for '{searchQuery}'", DateTime.Now));
        }
        catch (OperationCanceledException)
        {
            result.Cancelled = true;
            LogEntries.Add(new LogEntry($"Switch search cancelled for '{searchQuery}'", DateTime.Now));
        }
    }

    private bool CanPerformSwitchSearch()
    {
        return !string.IsNullOrEmpty(SwitchSearchText) && SwitchSearchText.Length >= MinLength;
    }

    private async Task PerformMergeSearchAsync()
    {
        var searchQuery = MergeSearchText;
        var result = new SearchResult
        {
            Query = searchQuery,
            Mode = "Merge",
            StartTime = DateTime.Now
        };

        MergeSearchResults.Add(result);
        LogEntries.Add(new LogEntry($"Merge search started for '{searchQuery}'", DateTime.Now));

        await Task.Delay(800);
        result.EndTime = DateTime.Now;
        SearchCount++;
        SearchResults = $"Merge search completed for '{searchQuery}' at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Merge search #{SearchCount} completed for '{searchQuery}'", DateTime.Now));
    }

    private bool CanPerformMergeSearch()
    {
        return !string.IsNullOrEmpty(MergeSearchText) && MergeSearchText.Length >= MinLength;
    }

    private void ClearResults()
    {
        SwitchSearchResults.Clear();
        MergeSearchResults.Clear();
        SearchCount = 0;
        SearchResults = "Results cleared";
        LogEntries.Add(new LogEntry("All search results cleared", DateTime.Now));
    }

    private bool CanClearResults()
    {
        return SwitchSearchResults.Count > 0 || MergeSearchResults.Count > 0;
    }
}
