using RxBlazorV2.Model;
using RxBlazorV2.Interface;
using RxBlazorV2.MudBlazor.Components;

namespace RxBlazorV2.MudBlazor.Sample.Model;

[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class ButtonDemoModel : ObservableModel
{
    public partial ButtonDemoModel(StatusModel statusModel);
    
    public partial int Counter { get; set; }
    
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
        StatusModel.AddMessage($"Incremented to {Counter}");
    }

    private void AddValueSync(int value)
    {
        Counter += value;
        if (Counter is >= 30 and <= 50)
        {
            throw new InvalidOperationException("Counter is between 30 and 50!");
        }
        StatusModel.AddMessage($"Added {value}, now {Counter}");
    }

    private async Task IncrementAsync()
    {
        StatusModel.AddMessage("Processing...");
        await Task.Delay(1000);
        Counter++;
        StatusModel.AddMessage($"Async increment to {Counter}");
    }

    private async Task LongOperationAsync(CancellationToken ct)
    {
        StatusModel.AddMessage("Long operation started...");

        for (var i = 1; i <= 5; i++)
        {
            StatusModel.AddMessage($"Step {i} of 5...");
            await Task.Delay(1000, ct);
        }

        Counter += 10;
        StatusModel.AddMessage($"Long operation completed! Counter: {Counter}");
    }

    private async Task AddValueAsync(int value, CancellationToken ct)
    {
        StatusModel.AddMessage($"Adding {value}...");
        await Task.Delay(1500, ct);
        Counter += value;
        StatusModel.AddMessage($"Added {value}, now {Counter}");
    }

    private bool CanIncrement() => Counter < 100;
}
