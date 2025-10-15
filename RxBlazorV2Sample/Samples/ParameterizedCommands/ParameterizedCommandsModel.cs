using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ParameterizedCommands;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ParameterizedCommandsModel : SampleBaseModel
{
    public override string Usage => "Commands can accept parameters of any type";
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(AddValueSync))]
    public partial IObservableCommand<int> AddCommand { get; }

    [ObservableCommand(nameof(AddValueAsync))]
    public partial IObservableCommandAsync<int> AddAsyncCommand { get; }

    [ObservableCommand(nameof(SetMessageSync))]
    public partial IObservableCommand<string> SetMessageCommand { get; }

    private void AddValueSync(int value)
    {
        Counter += value;
        LogEntries.Add(new LogEntry($"Added {value} synchronously, Counter is now {Counter}", DateTime.Now));
    }

    private async Task AddValueAsync(int value)
    {
        LogEntries.Add(new LogEntry($"Adding {value} asynchronously...", DateTime.Now));
        await Task.Delay(1000);
        Counter += value;
        LogEntries.Add(new LogEntry($"Added {value} asynchronously, Counter is now {Counter}", DateTime.Now));
    }

    private void SetMessageSync(string message)
    {
        LogEntries.Add(new LogEntry($"Message set: '{message}'", DateTime.Now));
    }
}
