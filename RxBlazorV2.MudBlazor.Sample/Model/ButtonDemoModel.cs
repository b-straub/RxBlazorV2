using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace RxBlazorV2.MudBlazor.Sample.Model;

[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class ButtonDemoModel : ObservableModel
{
    public partial int Counter { get; set; }

    public partial string StatusMessage { get; set; } = "Ready";
    
    public partial bool IndirectUsageReady { get; set; } = false;

    // Sync command - simple increment
    [ObservableCommand(nameof(IncrementSync), nameof(CanIncrement))]
    public partial IObservableCommand IncrementCommand { get; }

    // Sync command with parameter
    [ObservableCommand(nameof(AddValueSync), nameof(CanRun))]
    public partial IObservableCommand<int> AddCommand { get; }

    // Async command - simulates work
    [ObservableCommand(nameof(IncrementAsync), nameof(CanIncrement))]
    public partial IObservableCommandAsync IncrementAsyncCommand { get; }

    // Async command with cancellation support
    [ObservableCommand(nameof(LongOperationAsync), nameof(CanRun))]
    public partial IObservableCommandAsync LongOperationCommand { get; }

    // Async command with parameter
    [ObservableCommand(nameof(AddValueAsync), nameof(CanRun))]
    public partial IObservableCommandAsync<int> AddAsyncCommand { get; }

    protected override async Task OnContextReadyAsync()
    {
        await Task.Delay(3000);
        IndirectUsageReady = true;
    }

    private bool CanRun()
    {
        return IndirectUsageReady;
    }
    
    private void IncrementSync()
    {
        Counter++;
        StatusMessage = $"Incremented to {Counter}";
    }

    private void AddValueSync(int value)
    {
        Counter += value;
        StatusMessage = $"Added {value}, now {Counter}";
    }

    private async Task IncrementAsync()
    {
        StatusMessage = "Processing...";
        await Task.Delay(1000);
        Counter++;
        StatusMessage = $"Async increment to {Counter}";
    }

    private async Task LongOperationAsync(CancellationToken ct)
    {
        StatusMessage = "Long operation started...";

        for (var i = 1; i <= 5; i++)
        {
            StatusMessage = $"Step {i} of 5...";
            await Task.Delay(1000, ct);
        }

        Counter += 10;
        StatusMessage = $"Long operation completed! Counter: {Counter}";
    }

    private async Task AddValueAsync(int value, CancellationToken ct)
    {
        StatusMessage = $"Adding {value}...";
        await Task.Delay(1500, ct);
        Counter += value;
        StatusMessage = $"Added {value}, now {Counter}";
    }

    private bool CanIncrement() => Counter < 100;
}
