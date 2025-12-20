using StatusMessage = RxBlazorV2.Model.StatusMessage;
using StatusSeverity = RxBlazorV2.Model.StatusSeverity;

namespace ReactivePatternSample.Status.Models;

/// <summary>
/// Public API methods for AppStatusModel.
/// </summary>
public partial class AppStatusModel
{
    /// <summary>
    /// Adds an info message.
    /// </summary>
    public void AddInfo(string message, string? source = null)
    {
        AddMessage(message, StatusSeverity.Info, source);
    }

    /// <summary>
    /// Adds a success message.
    /// </summary>
    public void AddSuccess(string message, string? source = null)
    {
        AddMessage(message, StatusSeverity.Success, source);
    }

    /// <summary>
    /// Adds a warning message.
    /// </summary>
    public void AddWarning(string message, string? source = null)
    {
        AddMessage(message, StatusSeverity.Warning, source);
    }

    /// <summary>
    /// Adds an error message.
    /// </summary>
    public void AddError(string message, string? source = null)
    {
        AddMessage(message, StatusSeverity.Error, source);
    }

    /// <summary>
    /// Adds a message with the specified severity.
    /// </summary>
    private void AddMessage(string message, StatusSeverity severity, string? source)
    {
        var statusMessage = new StatusMessage(message, severity, source);
        Messages.Add(statusMessage);
        UpdateStatus();
    }

    /// <summary>
    /// Updates computed status properties.
    /// </summary>
    private void UpdateStatus()
    {
        HasErrors = Messages.Any(m => m.Severity == StatusSeverity.Error);
        HasWarnings = Messages.Any(m => m.Severity == StatusSeverity.Warning);
        MessageCount = Messages.Count;

        var lastErrorMsg = Messages.LastOrDefault(m => m.Severity == StatusSeverity.Error);
        LastError = lastErrorMsg?.Message;

        var lastMsg = Messages.LastOrDefault();
        LastMessage = lastMsg?.Message;
    }
}
