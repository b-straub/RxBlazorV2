using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ServiceModelInteraction;

/// <summary>
/// Model that reacts when ProcessingModel completes its work.
///
/// PATTERN: Internal observer watches ProcessingModel.Status
///
/// This is auto-detected by the generator because the private method
/// accesses ProcessingModel.Status. No attributes needed.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ResultsModel : ObservableModel
{
    public partial ResultsModel(ProcessingModel processingModel);

    /// <summary>
    /// Log entries for debugging the flow.
    /// </summary>
    public partial ObservableList<LogEntry> LogEntries { get; init; } = [];

    /// <summary>
    /// Count of successful reactions.
    /// </summary>
    public partial int SuccessReactionCount { get; set; }

    /// <summary>
    /// Count of error reactions.
    /// </summary>
    public partial int ErrorReactionCount { get; set; }

    /// <summary>
    /// Messages showing the reactive flow.
    /// </summary>
    public partial ObservableList<string> ReactionMessages { get; init; } = [];

    /// <summary>
    /// Simulated "loaded data" that would be refreshed on success.
    /// </summary>
    public partial string LoadedData { get; set; } = "Not loaded yet";

    /// <summary>
    /// Internal observer - AUTO-DETECTED because it accesses ProcessingModel.Status.
    /// Called automatically when ProcessingModel.Status changes.
    /// </summary>
    private void OnProcessingStatusChanged()
    {
        if (ProcessingModel.Status is null)
        {
            return;
        }

        var status = ProcessingModel.Status;
        var message = $"[{DateTime.Now:HH:mm:ss}] Reacted to Status: {status.Message}";
        ReactionMessages.Add(message);
        LogEntries.Add(new LogEntry(message, DateTime.Now));

        if (status.Severity == ProcessingSeverity.Success)
        {
            SuccessReactionCount++;
            // This is where you'd reload data, e.g.:
            // _ = LoadDataCommand.ExecuteAsync();
            LoadedData = $"Reloaded at {DateTime.Now:HH:mm:ss} (reaction #{SuccessReactionCount})";
            LogEntries.Add(new LogEntry("SUCCESS: Would reload data here", DateTime.Now));
        }
        else
        {
            ErrorReactionCount++;
            LogEntries.Add(new LogEntry("ERROR: Processing failed, not reloading", DateTime.Now));
        }
    }
}
