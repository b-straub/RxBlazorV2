using R3;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ModelObservers;

/// <summary>
/// Simple model with a timer-triggered counter property.
/// Used to demonstrate internal model observers in ModelObserversModel.
/// </summary>
[ObservableModelScope(ModelScope.Scoped)]
public partial class TimerSettingsModel : ObservableModel
{
    private IDisposable? _timerSubscription;

    public partial int TickCount { get; set; }

    public partial bool IsRunning { get; set; }

    public void StartTimer()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        _timerSubscription = R3.Observable.Interval(TimeSpan.FromSeconds(2))
            .Subscribe(_ => TickCount++);
    }

    public void StopTimer()
    {
        _timerSubscription?.Dispose();
        _timerSubscription = null;
        IsRunning = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timerSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}
