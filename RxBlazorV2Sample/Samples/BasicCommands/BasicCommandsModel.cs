using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.BasicCommands;

[ObservableModelScope(ModelScope.Scoped)]
public partial class BasicCommandsModel : SampleBaseModel
{ 
    public override string Usage => "Click a button to execute a command";
    public partial int Counter { get; set; }
   
    [ObservableCommand(nameof(IncrementSync))]
    public partial IObservableCommand IncrementCommand { get; }

    [ObservableCommand(nameof(IncrementAsync))]
    public partial IObservableCommandAsync IncrementAsyncCommand { get; }

    private void IncrementSync()
    {
        Counter++;
        LogEntries.Add(new LogEntry("Sync command executed", DateTime.Now));
    }

    private async Task IncrementAsync()
    {
        LogEntries.Add(new LogEntry("Async command started...", DateTime.Now));
        await Task.Delay(1000);
        Counter++;
        LogEntries.Add(new LogEntry("Async command completed", DateTime.Now));
    }
}
