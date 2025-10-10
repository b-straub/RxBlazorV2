using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.CrossComponentCommunication;

[ObservableModelScope(ModelScope.Singleton)]
public partial class CrossComponentCommunicationModel : SampleBaseModel
{
    public override string Usage => "Singleton models enable data sharing and communication across multiple components";
    public partial int SharedCounter { get; set; }
    public partial string SharedMessage { get; set; } = "Hello from shared model!";
    public partial bool IsActive { get; set; } = true;

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommand IncrementCommand { get; }

    [ObservableCommand(nameof(Decrement), nameof(CanDecrement))]
    public partial IObservableCommand DecrementCommand { get; }

    [ObservableCommand(nameof(Reset))]
    public partial IObservableCommand ResetCommand { get; }

    [ObservableCommand(nameof(ToggleActive))]
    public partial IObservableCommand ToggleActiveCommand { get; }

    private void Increment()
    {
        SharedCounter++;
        LogEntries.Add(new LogEntry($"Counter incremented to {SharedCounter}", DateTime.Now));
    }

    private void Decrement()
    {
        SharedCounter--;
        LogEntries.Add(new LogEntry($"Counter decremented to {SharedCounter}", DateTime.Now));
    }

    private bool CanDecrement()
    {
        return SharedCounter > 0;
    }

    private void Reset()
    {
        SharedCounter = 0;
        SharedMessage = "Counter has been reset!";
        LogEntries.Add(new LogEntry("Counter reset to 0", DateTime.Now));
    }

    private void ToggleActive()
    {
        IsActive = !IsActive;
        SharedMessage = IsActive ? "System is now active" : "System is now inactive";
        LogEntries.Add(new LogEntry(SharedMessage, DateTime.Now));
    }
}
