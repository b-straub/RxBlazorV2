using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace RxBlazorV2.MudBlazor.Sample.Model;

[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class ButtonDemoModel : ObservableModel
{
    public partial int Counter { get; set; }

    public partial string StatusMessage { get; set; } = "Ready";

    // Sync command - simple increment
    [ObservableCommand(nameof(IncrementSync))]
    public partial IObservableCommand IncrementCommand { get; }

    // Sync command with parameter
    [ObservableCommand(nameof(AddValueSync))]
    public partial IObservableCommand<int> AddCommand { get; }

    // Async command - simulates work
    [ObservableCommand(nameof(IncrementAsync))]
    public partial IObservableCommandAsync IncrementAsyncCommand { get; }

    // Async command with cancellation support
    [ObservableCommand(nameof(LongOperationAsync))]
    public partial IObservableCommandAsync LongOperationCommand { get; }

    // Async command with parameter
    [ObservableCommand(nameof(AddValueAsync))]
    public partial IObservableCommandAsync<int> AddAsyncCommand { get; }

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

    public bool CanIncrement() => Counter < 100;

    public bool CanDecrement() => Counter > 0;
}
