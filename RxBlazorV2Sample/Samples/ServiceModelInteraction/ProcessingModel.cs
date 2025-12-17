using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ServiceModelInteraction;

/// <summary>
/// Status severity for processing results.
/// </summary>
public enum ProcessingSeverity { Success, Error }

/// <summary>
/// Status message from processing.
/// </summary>
public record ProcessingStatus(string Message, ProcessingSeverity Severity);

/// <summary>
/// Demonstrates the correct service-model interaction pattern.
///
/// PATTERN: Property → Command → Service → Status → Other Model Reacts
///
/// 1. Property changes (e.g., user submits input)
/// 2. [ObservableCommandTrigger] auto-executes command
/// 3. Command method calls injected service
/// 4. Command sets Status property on completion
/// 5. Other models observe Status via internal observer and react
///
/// NO external observer needed. NO toggle properties. NO callbacks.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ProcessingModel : ObservableModel
{
    public partial ProcessingModel(ProcessingService processingService);

    /// <summary>
    /// Log entries for debugging the flow.
    /// </summary>
    public partial ObservableList<LogEntry> LogEntries { get; init; } = [];

    /// <summary>
    /// Input to be processed. Setting this triggers the command.
    /// </summary>
    public partial string? InputToProcess { get; set; }
    
    /// <summary>
    /// Status message from last processing.
    /// This is the COMPLETION SIGNAL that other models observe.
    /// </summary>
    public partial ProcessingStatus? Status { get; set; }

    /// <summary>
    /// Last successfully processed item (for display).
    /// </summary>
    public partial ProcessedItem? LastResult { get; set; }

    /// <summary>
    /// History of all processed items.
    /// </summary>
    public partial ObservableList<ProcessedItem> ProcessedItems { get; init; } = [];

    /// <summary>
    /// Command auto-triggered when InputToProcess changes.
    /// The command owns the workflow - calls service and sets status.
    /// </summary>
    [ObservableCommand(nameof(ProcessInputAsync), nameof(CanProcess))]
    [ObservableCommandTrigger(nameof(InputToProcess))]
    public partial IObservableCommandAsync ProcessInputCommand { get; }

    private bool CanProcess()
    {
        return !string.IsNullOrWhiteSpace(InputToProcess) && InputToProcess.Length > 3;
    }
    
    private async Task ProcessInputAsync(CancellationToken ct)
    {
        LogEntries.Add(new LogEntry($"Command started for: '{InputToProcess}'", DateTime.Now));
        
        try
        {
            // Call injected service
            LogEntries.Add(new LogEntry("Calling service...", DateTime.Now));
            var result = await ProcessingService.ProcessAsync(InputToProcess!, ct);
            LogEntries.Add(new LogEntry("Service returned", DateTime.Now));

            // Store result
            LastResult = result;
            ProcessedItems.Add(result);

            // Set status - THIS IS THE COMPLETION SIGNAL
            Status = new ProcessingStatus($"Processed: {result.Result}", ProcessingSeverity.Success);
            LogEntries.Add(new LogEntry("Command completed successfully", DateTime.Now));
        }
        catch (OperationCanceledException)
        {
            var reason = ProcessInputCommand.LastCancellationReason;
              // Only show error status for explicit cancellation, not for Switch
            if (reason == CancellationReason.EXPLICIT)
            {
                LogEntries.Add(new LogEntry($"Command was CANCELLED (Reason: {reason})", DateTime.Now));
                Status = new ProcessingStatus("Command was CANCELED", ProcessingSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            Status = new ProcessingStatus($"Error: {ex.Message}", ProcessingSeverity.Error);
            LogEntries.Add(new LogEntry($"Command failed: {ex.Message}", DateTime.Now));
        }
    }
}
