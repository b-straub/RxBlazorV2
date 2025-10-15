using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.CommandsWithCanExecute;

[ObservableComponent]

[ObservableModelScope(ModelScope.Singleton)]
public partial class CommandsWithCanExecuteModel : SampleBaseModel
{
    public override string Usage => "Commands can be enabled/disabled based on model state using CanExecute";
    public partial int Counter { get; set; }
    public partial bool IsEnabled { get; set; } = true;
    public partial string Message { get; set; } = "Commands enabled";

    [ObservableCommand(nameof(Increment), nameof(CanIncrement))]
    public partial IObservableCommand IncrementCommand { get; }

    [ObservableCommand(nameof(AddValue), nameof(CanAddValue))]
    public partial IObservableCommand<int> AddValueCommand { get; }

    [ObservableCommand(nameof(Reset), nameof(CanReset))]
    public partial IObservableCommand ResetCommand { get; }

    [ObservableCommand(nameof(ToggleEnabled))]
    public partial IObservableCommand ToggleEnabledCommand { get; }

    private void Increment()
    {
        Counter++;
        Message = $"Incremented to {Counter} at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Incremented to {Counter}", DateTime.Now));
    }

    private bool CanIncrement()
    {
        return IsEnabled && Counter < 10;
    }

    private void AddValue(int value)
    {
        Counter += value;
        Message = $"Added {value}, Counter is now {Counter}";
        LogEntries.Add(new LogEntry($"Added {value}, Counter is now {Counter}", DateTime.Now));
    }

    private bool CanAddValue()
    {
        return IsEnabled && Counter < 20;
    }

    private void Reset()
    {
        Counter = 0;
        Message = "Counter reset";
        LogEntries.Add(new LogEntry("Counter reset to 0", DateTime.Now));
    }

    private bool CanReset()
    {
        return Counter > 0;
    }

    private void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
        Message = IsEnabled ? "Commands enabled" : "Commands disabled";
        LogEntries.Add(new LogEntry(Message, DateTime.Now));
    }
}
