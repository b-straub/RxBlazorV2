using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Transient)]
public partial class TestReturnCommandModel : ObservableModel
{
    public partial int Value { get; set; }

    [ObservableCommand(nameof(ExecuteSyncReturn), nameof(CanExecute))]
    public partial IObservableCommandR<int?> SyncReturnCommand { get; }

    [ObservableCommand(nameof(ExecuteAsyncReturn), nameof(CanExecute))]
    public partial IObservableCommandRAsync<int?> AsyncReturnCommand { get; }

    [ObservableCommand(nameof(ExecuteAsyncCancelableReturn), nameof(CanExecute))]
    public partial IObservableCommandRAsync<int?> CancelableReturnCommand { get; }

    [ObservableCommand(nameof(ExecuteSyncReturnWithParam), nameof(CanExecute))]
    public partial IObservableCommandR<int, string?> SyncReturnCommandWithParam { get; }

    [ObservableCommand(nameof(ExecuteAsyncReturnWithParam), nameof(CanExecute))]
    public partial IObservableCommandRAsync<int, string?> AsyncReturnCommandWithParam { get; }

    [ObservableCommand(nameof(ExecuteAsyncCancelableReturnWithParam), nameof(CanExecute))]
    public partial IObservableCommandRAsync<int, string?> CancelableReturnCommandWithParam { get; }

    public int ExecuteCount { get; private set; }
    public int LastParameter { get; private set; }
    public bool ThrowException { get; set; }
    public List<string> ExecutionLog { get; } = new();

    private bool CanExecute()
    {
        return Value >= 0;
    }

    private int? ExecuteSyncReturn()
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception in sync return");
        }
        ExecuteCount++;
        Value++;
        ExecutionLog.Add($"SyncReturn: Value={Value}");
        return Value * 10;
    }

    private async Task<int?> ExecuteAsyncReturn()
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception in async return");
        }
        await Task.Delay(10, CancellationToken.None);
        ExecuteCount++;
        Value++;
        ExecutionLog.Add($"AsyncReturn: Value={Value}");
        return Value * 10;
    }

    private async Task<int?> ExecuteAsyncCancelableReturn(CancellationToken token)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception in cancelable return");
        }
        ExecutionLog.Add("CancelableReturn: Started");
        await Task.Delay(100, token);
        ExecuteCount++;
        Value++;
        ExecutionLog.Add($"CancelableReturn: Completed, Value={Value}");
        return Value * 10;
    }

    private string? ExecuteSyncReturnWithParam(int param)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception in sync return with param");
        }
        ExecuteCount++;
        LastParameter = param;
        Value += param;
        ExecutionLog.Add($"SyncReturnWithParam: param={param}, Value={Value}");
        return $"Result: {Value}";
    }

    private async Task<string?> ExecuteAsyncReturnWithParam(int param)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception in async return with param");
        }
        await Task.Delay(10, CancellationToken.None);
        ExecuteCount++;
        LastParameter = param;
        Value += param;
        ExecutionLog.Add($"AsyncReturnWithParam: param={param}, Value={Value}");
        return $"Result: {Value}";
    }

    private async Task<string?> ExecuteAsyncCancelableReturnWithParam(int param, CancellationToken token)
    {
        if (ThrowException)
        {
            throw new InvalidOperationException("Test exception in cancelable return with param");
        }
        ExecutionLog.Add($"CancelableReturnWithParam: Started, param={param}");
        await Task.Delay(100, token);
        ExecuteCount++;
        LastParameter = param;
        Value += param;
        ExecutionLog.Add($"CancelableReturnWithParam: Completed, param={param}, Value={Value}");
        return $"Result: {Value}";
    }
}
