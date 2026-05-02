namespace RxBlazorV2.CoreTests.TestFixtures;

/// <summary>
/// Test component overriding the cancellation-aware <c>OnContextReadyAsync(CancellationToken)</c> overload.
/// Exercises the component-side disposal-cancels-token path.
/// </summary>
public partial class CancellationAwareComponent
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task EnteredAsync => _entered.Task;
    public bool DelayCompleted { get; private set; }
    public bool TokenWasCancelled { get; private set; }
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
