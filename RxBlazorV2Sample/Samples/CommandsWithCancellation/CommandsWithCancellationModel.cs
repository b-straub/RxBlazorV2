using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.CommandsWithCancellation;

[ObservableComponent]

[ObservableModelScope(ModelScope.Singleton)]
public partial class CommandsWithCancellationModel : SampleBaseModel
{
    public override string Usage => "Long-running async commands can be cancelled using CancellationToken";
    public partial int Progress { get; set; }
    public partial string Status { get; set; } = "Ready";
    public partial bool IsCancellable { get; set; } = true;

    [ObservableCommand(nameof(LongRunningOperationAsync))]
    public partial IObservableCommandAsync LongOperationCommand { get; }

    [ObservableCommand(nameof(LongRunningOperationWithParameterAsync))]
    public partial IObservableCommandAsync<int> LongOperationWithParamCommand { get; }

    private async Task LongRunningOperationAsync(CancellationToken cancellationToken)
    {
        Status = "Operation started...";
        Progress = 0;
        LogEntries.Add(new LogEntry("Long operation started", DateTime.Now));

        try
        {
            for (var i = 1; i <= 10; i++)
            {
                await Task.Delay(500, cancellationToken);
                Progress = i * 10;
                Status = $"Processing... {Progress}%";
            }

            Status = "Operation completed successfully!";
            LogEntries.Add(new LogEntry("Long operation completed successfully", DateTime.Now));
        }
        catch (OperationCanceledException)
        {
            Status = "Operation was cancelled";
            Progress = 0;
            LogEntries.Add(new LogEntry("Long operation was cancelled", DateTime.Now));
        }
    }

    private async Task LongRunningOperationWithParameterAsync(int iterations, CancellationToken cancellationToken)
    {
        Status = $"Starting operation with {iterations} iterations...";
        Progress = 0;
        LogEntries.Add(new LogEntry($"Parameterized operation started with {iterations} iterations", DateTime.Now));

        try
        {
            for (var i = 1; i <= iterations; i++)
            {
                await Task.Delay(1000, cancellationToken);
                Progress = (i * 100) / iterations;
                Status = $"Processing iteration {i}/{iterations}... {Progress}%";
            }

            Status = $"Completed all {iterations} iterations successfully!";
            LogEntries.Add(new LogEntry($"Completed all {iterations} iterations successfully", DateTime.Now));
        }
        catch (OperationCanceledException)
        {
            Status = $"Operation cancelled at iteration {Progress * iterations / 100}";
            Progress = 0;
            LogEntries.Add(new LogEntry($"Operation cancelled at {Progress}%", DateTime.Now));
        }
    }
}
