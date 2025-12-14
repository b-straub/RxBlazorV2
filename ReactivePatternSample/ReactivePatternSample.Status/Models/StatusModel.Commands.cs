using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Status.Models;

/// <summary>
/// Commands and their implementations for StatusModel.
/// </summary>
public partial class StatusModel
{
    /// <summary>
    /// Command to clear all messages.
    /// </summary>
    [ObservableCommand(nameof(ClearAllMessages), nameof(CanClearMessages))]
    public partial IObservableCommand ClearCommand { get; }

    /// <summary>
    /// Command to toggle the status panel.
    /// </summary>
    [ObservableCommand(nameof(TogglePanel))]
    public partial IObservableCommand TogglePanelCommand { get; }

    /// <summary>
    /// Clears all messages.
    /// </summary>
    private void ClearAllMessages()
    {
        Messages.Clear();
        UpdateStatus();
    }

    /// <summary>
    /// Can clear messages if there are any.
    /// </summary>
    private bool CanClearMessages() => Messages.Count > 0;

    /// <summary>
    /// Toggles the status panel expanded state.
    /// </summary>
    private void TogglePanel()
    {
        IsPanelExpanded = !IsPanelExpanded;
    }
}
