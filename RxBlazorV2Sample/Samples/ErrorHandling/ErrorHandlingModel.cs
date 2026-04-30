using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;
using RxBlazorVSSampleComponents.ErrorManager;

namespace RxBlazorV2Sample.Samples.ErrorHandling;

/// <summary>
/// Demonstrates automatic error handling via StatusModel.
/// When a StatusModel is injected, all command exceptions are automatically
/// captured with source info (command name + method name) and routed to the status handler.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorHandlingModel : SampleBaseModel
{
    // Inject StatusModel to enable automatic error capture for all commands
    public partial ErrorHandlingModel(ErrorModel errorModel);

    public override string Usage => "Click buttons to trigger commands - errors are automatically captured";

    public partial int Counter { get; set; }

    /// <summary>
    /// Command that always succeeds.
    /// </summary>
    [ObservableCommand(nameof(IncrementSync))]
    public partial IObservableCommand IncrementCommand { get; }

    /// <summary>
    /// Command that throws when counter reaches certain values.
    /// The exception is automatically captured by StatusModel with source: "RiskyIncrementCommand.RiskyIncrement".
    /// </summary>
    [ObservableCommand(nameof(RiskyIncrement))]
    public partial IObservableCommand RiskyIncrementCommand { get; }

    /// <summary>
    /// Async command that throws after a delay.
    /// The exception is automatically captured by StatusModel with source: "RiskyOperationCommand.RiskyOperationAsync".
    /// </summary>
    [ObservableCommand(nameof(RiskyOperationAsync))]
    public partial IObservableCommandAsync RiskyOperationCommand { get; }

    /// <summary>
    /// Async command that throws different exception types and routes them through a per-command
    /// error formatter. The formatter pattern-matches on the exception type to produce a friendly
    /// message; the framework forwards that message to StatusModel (and also exposes it on
    /// <c>Command.ErrorMessage</c>). Source: "FetchDataCommand.FetchDataAsync".
    /// </summary>
    [ObservableCommand(nameof(FetchDataAsync), null, nameof(FormatFetchError))]
    public partial IObservableCommandAsync FetchDataCommand { get; }

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

    /// <summary>
    /// Throws either a <see cref="TimeoutException"/> or an <see cref="InvalidOperationException"/>
    /// depending on parity. The cryptic raw <c>ex.Message</c> never reaches the user — the formatter
    /// rewrites it.
    /// </summary>
    private async Task FetchDataAsync()
    {
        Counter++;
        LogEntries.Add(new LogEntry($"Fetching data (attempt {Counter})...", DateTime.Now));
        await Task.Delay(300);

        if (Counter % 2 == 0)
        {
            throw new TimeoutException("HTTP timeout: 0x80072EE2 connection reset by peer");
        }

        throw new InvalidOperationException("NRE in DataMapper.Bind: Object reference not set to an instance of an object");
    }

    /// <summary>
    /// Maps fetch-data exceptions to user-facing text. Pattern matching keeps the friendly
    /// message close to the failure mode and hides cryptic internals from the status display.
    /// </summary>
    private string FormatFetchError(Exception ex) => ex switch
    {
        TimeoutException          => "The data service is unreachable. Please check your connection and try again.",
        InvalidOperationException => "Could not load data: the response was incomplete.",
        _                         => $"Could not load data: {ex.Message}",
    };
}
