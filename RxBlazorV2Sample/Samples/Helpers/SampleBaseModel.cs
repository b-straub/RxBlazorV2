using ObservableCollections;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.Helpers;

public abstract partial class SampleBaseModel : ObservableModel
{
    public abstract string Usage { get; }
    public required partial ObservableList<LogEntry> LogEntries { get; init; }
} 