using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.MudBlazor.Components;

[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class StatusModel : ObservableModel, IErrorModel
{
    [ObservableComponentTrigger]
    public ObservableList<string> Errors { get; } = [];

    [ObservableComponentTrigger]
    public ObservableList<string> Messages { get; } = [];

    public StatusMessageMode ErrorMode { get; set; } = StatusMessageMode.AGGREGATE;
    public StatusMessageMode MessageMode { get; set; } = StatusMessageMode.SINGLE;

    public void HandleError(Exception error)
    {
        if (ErrorMode is StatusMessageMode.SINGLE)
        {
            Errors.Clear();
        }
        Errors.Add(error.Message);
    }

    public void AddMessage(string message)
    {
        if (MessageMode is StatusMessageMode.SINGLE)
        {
            Messages.Clear();
        }
        Messages.Add(message);
    }
}