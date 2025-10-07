using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Transient)]
public partial class TestCommandModel : ObservableModel
{
    public partial int Value { get; set; }

    [ObservableCommand(nameof(ExecuteSync), nameof(CanExecute))]
    public partial IObservableCommand SyncCommand { get; }

    [ObservableCommand(nameof(ExecuteSyncWithParam), nameof(CanExecute))]
    public partial IObservableCommand<int> SyncCommandWithParam { get; }

    [ObservableCommand(nameof(ExecuteAsync), nameof(CanExecute))]
    public partial IObservableCommandAsync AsyncCommand { get; }

    [ObservableCommand(nameof(ExecuteAsyncWithParam), nameof(CanExecute))]
    public partial IObservableCommandAsync<int> AsyncCommandWithParam { get; }

    [ObservableCommand(nameof(ExecuteAsyncCancelable), nameof(CanExecute))]
    public partial IObservableCommandAsync CancelableCommand { get; }

    [ObservableCommand(nameof(ExecuteAsyncCancelableWithParam), nameof(CanExecute))]
    public partial IObservableCommandAsync<int> CancelableAsyncCommandWithParam { get; }

    public int ExecuteCount { get; private set; }
    public int LastParameter { get; private set; }
    public bool ThrowException { get; set; }

    private bool CanExecute()
    {
        return Value >= 0;
    }

    private void ExecuteSync()
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        ExecuteCount++;
        Value++;
    }

    private void ExecuteSyncWithParam(int param)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        ExecuteCount++;
        LastParameter = param;
        Value += param;
    }

    private async Task ExecuteAsync()
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        await Task.Delay(10);
        ExecuteCount++;
        Value++;
    }

    private async Task ExecuteAsyncWithParam(int param)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        await Task.Delay(10);
        ExecuteCount++;
        LastParameter = param;
        Value += param;
    }

    private async Task ExecuteAsyncCancelable(CancellationToken token)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        await Task.Delay(100, token);
        ExecuteCount++;
        Value++;
    }

    private async Task ExecuteAsyncCancelableWithParam(int param, CancellationToken token)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        await Task.Delay(100, token);
        ExecuteCount++;
        LastParameter = param;
        Value += param;
    }

    public void TriggerStateChanged(string propertyName)
    {
        StateHasChanged(propertyName);
    }
}
