using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ParameterizedCommandsWithReturn;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ParameterizedCommandsRModel : SampleBaseModel
{
    public override string Usage => "Commands can accept parameters and return values";
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(CalculateSync))]
    public partial IObservableCommandR<int, double> CalculateCommand { get; }

    [ObservableCommand(nameof(CalculateAsync))]
    public partial IObservableCommandRAsync<int, double> CalculateAsyncCommand { get; }

    [ObservableCommand(nameof(FormatSync))]
    public partial IObservableCommandR<string, string> FormatCommand { get; }

    [ObservableCommand(nameof(FormatAsync))]
    public partial IObservableCommandRAsync<string, string?> FormatAsyncCommand { get; }

    private double CalculateSync(int value)
    {
        Counter += value;
        var result = Counter * 1.5;
        LogEntries.Add(new LogEntry($"Calculated {value} * 1.5 = {result} synchronously, Counter is now {Counter}", DateTime.Now));
        return result;
    }

    private async Task<double> CalculateAsync(int value)
    {
        LogEntries.Add(new LogEntry($"Calculating {value} * 1.5 asynchronously...", DateTime.Now));
        await Task.Delay(1000);
        Counter += value;
        var result = Counter * 1.5;
        LogEntries.Add(new LogEntry($"Calculated {value} * 1.5 = {result} asynchronously, Counter is now {Counter}", DateTime.Now));
        return result;
    }

    private string FormatSync(string message)
    {
        var formatted = $"[{DateTime.Now:HH:mm:ss}] {message.ToUpper()}";
        LogEntries.Add(new LogEntry($"Formatted message synchronously: {formatted}", DateTime.Now));
        return formatted;
    }

    private async Task<string?> FormatAsync(string message)
    {
        LogEntries.Add(new LogEntry($"Formatting message asynchronously...", DateTime.Now));
        await Task.Delay(500);
        var formatted = $"[{DateTime.Now:HH:mm:ss}] {message.ToUpper()}";
        LogEntries.Add(new LogEntry($"Formatted message asynchronously: {formatted}", DateTime.Now));
        return formatted;
    }
}
