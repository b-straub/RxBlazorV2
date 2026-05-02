using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

/// <summary>
/// Test fixture overriding the cancellation-aware <c>OnContextReadyAsync(CancellationToken)</c> overload.
/// Exercises the path where the token is bound to model disposal — used by tests that dispose the model
/// mid-init and assert the awaitable cancels.
/// </summary>
[ObservableModelScope(ModelScope.Transient)]
public partial class CancellationAwareModel : ObservableModel
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once <c>OnContextReadyAsync(CancellationToken)</c> has been entered.</summary>
    public Task EnteredAsync => _entered.Task;

    /// <summary>True if the awaited delay completed without cancellation.</summary>
    public bool DelayCompleted { get; private set; }

    /// <summary>True if an <see cref="OperationCanceledException"/> was observed inside the override.</summary>
    public bool TokenWasCancelled { get; private set; }

    /// <summary>Duration of the simulated async work.</summary>
    public TimeSpan DelayDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    protected override async Task OnContextReadyAsync(CancellationToken cancellationToken)
    {
        _entered.TrySetResult();
        try
        {
            await Task.Delay(DelayDuration, cancellationToken);
            DelayCompleted = true;
        }
        catch (OperationCanceledException)
        {
            TokenWasCancelled = true;
            throw;
        }
    }
}
