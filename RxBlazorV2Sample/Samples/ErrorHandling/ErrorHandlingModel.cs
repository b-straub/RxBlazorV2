using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ErrorHandling;

/// <summary>
/// Demonstrates automatic error handling via IErrorModel.
/// When IErrorModel is injected, all command exceptions are automatically
/// captured and routed to the error handler - no try/catch needed.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorHandlingModel : SampleBaseModel
{
    // Inject IErrorModel to enable automatic error capture for all commands
    public partial ErrorHandlingModel(IErrorModel errorModel);

    public override string Usage => "Click buttons to trigger commands - errors are automatically captured";

    public partial int Counter { get; set; }

    /// <summary>
    /// Command that always succeeds.
    /// </summary>
    [ObservableCommand(nameof(IncrementSync))]
    public partial IObservableCommand IncrementCommand { get; }

    /// <summary>
    /// Command that throws when counter reaches certain values.
    /// The exception is automatically captured by IErrorModel.
    /// </summary>
    [ObservableCommand(nameof(RiskyIncrement))]
    public partial IObservableCommand RiskyIncrementCommand { get; }

    /// <summary>
    /// Async command that throws after a delay.
    /// The exception is automatically captured by IErrorModel.
    /// </summary>
    [ObservableCommand(nameof(RiskyOperationAsync))]
    public partial IObservableCommandAsync RiskyOperationCommand { get; }

    private void IncrementSync()
    {
        Counter++;
        LogEntries.Add(new LogEntry($"Counter incremented to {Counter}", DateTime.Now));
    }

    private void RiskyIncrement()
    {
        Counter++;
        LogEntries.Add(new LogEntry($"Risky increment to {Counter}", DateTime.Now));

        // Throw when counter is between 5 and 10
        if (Counter is >= 5 and <= 10)
        {
            throw new InvalidOperationException($"Counter value {Counter} is in the danger zone (5-10)!");
        }
    }

    private async Task RiskyOperationAsync()
    {
        LogEntries.Add(new LogEntry("Starting risky async operation...", DateTime.Now));
        await Task.Delay(500);

        // Always throw to demonstrate async error capture
        throw new InvalidOperationException("Async operation failed! This error was automatically captured.");
    }
}
