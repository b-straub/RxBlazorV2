using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.CommandTriggers;

[ObservableModelScope(ModelScope.Scoped)]
public partial class CommandTriggersModel : SampleBaseModel
{
    public override string Usage => "Commands can automatically execute when specific properties change";
    public partial string SearchText { get; set; } = "";
    public partial int MinLength { get; set; } = 3;
    public partial string SearchResults { get; set; } = "No search performed yet";
    public partial int SearchCount { get; set; }
    public partial bool ParameterizedSearch { get; set; }

    [ObservableCommand(nameof(PerformSearchAsync), nameof(CanPerformSearch))]
    [ObservableCommandTrigger(nameof(SearchText))]
    public partial IObservableCommandAsync SearchCommand { get; }

    [ObservableCommand(nameof(PerformParametrizedSearchAsync), nameof(CanPerformParametrizedSearch))]
    [ObservableCommandTrigger<string>(nameof(SearchText), "auto")]
    public partial IObservableCommandAsync<string> ParametrizedSearchCommand { get; }

    private async Task PerformSearchAsync(CancellationToken ct)
    {
        SearchResults = $"Searching for '{SearchText}'...";
        LogEntries.Add(new LogEntry($"Auto-search triggered for '{SearchText}'", DateTime.Now));
        await Task.Delay(500, ct);
        SearchCount++;
        SearchResults = $"Search #{SearchCount}: Found results for '{SearchText}' at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Search #{SearchCount} completed", DateTime.Now));
    }

    private bool CanPerformSearch()
    {
        return !string.IsNullOrEmpty(SearchText) && SearchText.Length >= MinLength;
    }

    private async Task PerformParametrizedSearchAsync(string mode)
    {
        SearchResults = $"Searching for '{SearchText}' in {mode} mode...";
        LogEntries.Add(new LogEntry($"Parameterized search triggered in '{mode}' mode", DateTime.Now));
        await Task.Delay(500);
        SearchCount++;
        SearchResults = $"Search #{SearchCount}: Found results for '{SearchText}' in {mode} mode at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Search #{SearchCount} completed in {mode} mode", DateTime.Now));
    }
    
    private bool CanPerformParametrizedSearch()
    {
        return !string.IsNullOrEmpty(SearchText) && ParameterizedSearch;
    }
}
