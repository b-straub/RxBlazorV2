using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

/// <summary>
/// Records the outcome of every individual run of a cancellable command, so a test can assert
/// which run was cancelled rather than only that nothing completed.
/// </summary>
[ObservableModelScope(ModelScope.Transient)]
public partial class SwitchCancellationModel : ObservableModel
{
    public partial int Value { get; set; }

    [ObservableCommand(nameof(RunAsync))]
    public partial IObservableCommandAsync RunCommand { get; }

    [ObservableCommand(nameof(RunWithParamAsync))]
    public partial IObservableCommandAsync<int> RunWithParamCommand { get; }

    [ObservableCommand(nameof(RunReturnAsync))]
    public partial IObservableCommandRAsync<int?> RunReturnCommand { get; }

    [ObservableCommand(nameof(RunReturnWithParamAsync))]
    public partial IObservableCommandRAsync<int, int?> RunReturnWithParamCommand { get; }

    /// <summary>
    /// One-based index of every run that reached its end without observing cancellation.
    /// </summary>
    public List<int> Completed { get; } = [];

    /// <summary>
    /// One-based index of every run that observed cancellation.
    /// </summary>
    public List<int> Cancelled { get; } = [];

    /// <summary>
    /// How long each run parks on its await - long enough for a test to cancel it mid-flight.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 2000;

    private int _runs;

    private async Task RunAsync(CancellationToken token)
    {
        await TrackAsync(token);
    }

    private async Task RunWithParamAsync(int param, CancellationToken token)
    {
        await TrackAsync(token, param);
    }

    private async Task<int?> RunReturnAsync(CancellationToken token)
    {
        await TrackAsync(token);
        return Value;
    }

    private async Task<int?> RunReturnWithParamAsync(int param, CancellationToken token)
    {
        await TrackAsync(token, param);
        return Value;
    }

    private async Task TrackAsync(CancellationToken token, int increment = 1)
    {
        var run = ++_runs;

        try
        {
            await Task.Delay(DelayMilliseconds, token);
        }
        catch (OperationCanceledException)
        {
            Cancelled.Add(run);
            throw;
        }

        Completed.Add(run);
        Value += increment;
    }
}
