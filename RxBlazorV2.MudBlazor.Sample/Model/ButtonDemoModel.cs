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
        StatusModel.AddInfo($"Incremented to {Counter}");
    }

    private void AddValueSync(int value)
    {
        Counter += value;
        switch (Counter)
        {
            case > 50:
                throw new InvalidOperationException("Counter is over 50!");
            case >= 30:
                StatusModel.AddWarning($"Added {value}, now {Counter}, approaching limit!", "AddValueSync");
                break;
            case >= 25:
                StatusModel.AddWarning($"Added {value}, now {Counter}, still right but approaching limit!", "AddValueSync");
                break;
            case >= 15:
                StatusModel.AddSuccess($"Added {value}, now {Counter}, just right!", "AddValueSync");
                break;
            default:
                StatusModel.AddInfo($"Added {value}, now {Counter}, still below 15.", "AddValueSync");
                break;
        }
    }

    private async Task IncrementAsync()
    {
        StatusModel.AddInfo("Processing...");
        await Task.Delay(1000);
        Counter++;
        StatusModel.AddInfo($"Async increment to {Counter}");
    }

    private async Task LongOperationAsync(CancellationToken ct)
    {
        StatusModel.AddInfo("Long operation started...");

        for (var i = 1; i <= 5; i++)
        {
            StatusModel.AddInfo($"Step {i} of 5...");
            await Task.Delay(1000, ct);
        }

        Counter += 10;
        StatusModel.AddInfo($"Long operation completed! Counter: {Counter}");
    }

    private async Task AddValueAsync(int value, CancellationToken ct)
    {
        StatusModel.AddInfo($"Adding {value}...");
        await Task.Delay(1500, ct);
        Counter += value;
        StatusModel.AddInfo($"Added {value}, now {Counter}");
    }

    private bool CanIncrement() => Counter < 100;
}
