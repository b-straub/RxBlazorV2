using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.BasicCommands;

[ObservableComponent]

[ObservableModelScope(ModelScope.Singleton)]
public partial class BasicCommandsRModel : SampleBaseModel
{ 
    public override string Usage => "Click a button to execute a command";
    public partial int Counter { get; set; }
   
    [ObservableCommand(nameof(IncrementSync))]
    public partial IObservableCommandR<int?> IncrementCommand { get; }

    [ObservableCommand(nameof(IncrementAsync))]
    public partial IObservableCommandRAsync<int?> IncrementAsyncCommand { get; }

    private int? IncrementSync()
    {
        Counter++;
        LogEntries.Add(new LogEntry("Sync command executed", DateTime.Now));
        return Counter * 10;
    }

    private async Task<int?> IncrementAsync()
    {
        LogEntries.Add(new LogEntry("Async command started...", DateTime.Now));
        await Task.Delay(1000);
        Counter++;
        LogEntries.Add(new LogEntry("Async command completed", DateTime.Now));
        return Counter * 10;
    }
}
