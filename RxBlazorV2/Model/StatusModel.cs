using ObservableCollections;

namespace RxBlazorV2.Model;

/// <summary>
/// Abstract base class for status management models.
/// Provides unified error and message handling with severity levels and source tracking.
///
/// Derive from this class and add [ObservableComponent] and [ObservableModelScope] attributes
/// to create a concrete singleton status model for your application.
///
/// Example:
/// <code>
/// [ObservableComponent]
/// [ObservableModelScope(ModelScope.Singleton)]
/// public partial class AppStatusModel : StatusBaseModel { }
/// </code>
/// </summary>
public abstract class StatusBaseModel : ObservableModel
{
    /// <summary>
    /// All status messages including errors, warnings, info, and success messages.
    /// Changes to this collection trigger component updates via [ObservableComponentTrigger].
    /// </summary>
    [ObservableComponentTrigger]
    public abstract ObservableList<StatusMessage> Messages { get; }

    /// <summary>
    /// How error messages are accumulated - Aggregate (multiple) or Single (replace).
    /// </summary>
    public StatusMessageMode ErrorMessageMode { get; set; } = StatusMessageMode.Aggregate;

    /// <summary>
    /// How non-error messages (Info, Success, Warning) are accumulated - Aggregate (multiple) or Single (replace).
    /// </summary>
    public StatusMessageMode MessageMessageMode { get; set; } = StatusMessageMode.Aggregate;

    /// <summary>
    /// Called by command factories when a command throws an exception.
    /// Automatically captures command name and method name as source.
    /// </summary>
    /// <param name="error">The exception that was thrown.</param>
    /// <param name="commandName">The name of the command property (e.g., "RefreshCommand").</param>
    /// <param name="methodName">The name of the execute method (e.g., "RefreshDataAsync").</param>
    public void HandleError(Exception error, string commandName, string methodName)
    {
        var source = $"{commandName}.{methodName}";
        AddError(error.Message, source);
    }

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
    /// Adds an error message.
    /// </summary>
    public void AddError(Exception ex, string? source = null)
    {
        AddMessage(ex.Message, StatusSeverity.Error, source);
    }

    /// <summary>
    /// Adds a message with the specified severity.
    /// </summary>
    protected void AddMessage(string message, StatusSeverity severity, string? source)
    {
        var mode = severity == StatusSeverity.Error ? ErrorMessageMode : MessageMessageMode;

        if (mode is StatusMessageMode.Single)
        {
            // Clear only messages of the same category (errors vs non-errors)
            if (severity == StatusSeverity.Error)
            {
                ClearMessages(StatusSeverity.Error);
            }
            else
            {
                ClearNonErrorMessages();
            }
        }

        Messages.Add(new StatusMessage(message, severity, source));
    }

    /// <summary>
    /// Clears all non-error messages (Info, Success, Warning).
    /// </summary>
    public void ClearNonErrorMessages()
    {
        var toRemove = Messages.Where(m => m.Severity is not StatusSeverity.Error).ToList();
        foreach (var msg in toRemove)
        {
            Messages.Remove(msg);
        }
    }
    
    /// <summary>
    /// Clears all error messages (Error).
    /// </summary>
    public void ClearErrorMessages()
    {
        var toRemove = Messages.Where(m => m.Severity is StatusSeverity.Error).ToList();
        foreach (var msg in toRemove)
        {
            Messages.Remove(msg);
        }
    }

    /// <summary>
    /// Clears all messages.
    /// </summary>
    public void ClearMessages()
    {
        Messages.Clear();
    }

    /// <summary>
    /// Clears messages with the specified severity.
    /// </summary>
    public void ClearMessages(StatusSeverity severity)
    {
        var toRemove = Messages.Where(m => m.Severity == severity).ToList();
        foreach (var msg in toRemove)
        {
            Messages.Remove(msg);
        }
    }

    /// <summary>
    /// Indicates whether there are any error messages.
    /// </summary>
    public bool HasErrors => Messages.Any(m => m.Severity == StatusSeverity.Error);

    /// <summary>
    /// Indicates whether there are any warning messages.
    /// </summary>
    public bool HasWarnings => Messages.Any(m => m.Severity == StatusSeverity.Warning);

    /// <summary>
    /// Count of error messages.
    /// </summary>
    public int ErrorCount => Messages.Count(m => m.Severity == StatusSeverity.Error);

    /// <summary>
    /// Count of warning messages.
    /// </summary>
    public int WarningCount => Messages.Count(m => m.Severity == StatusSeverity.Warning);

    /// <summary>
    /// The most recent error message, if any.
    /// </summary>
    public StatusMessage? LastError => Messages.LastOrDefault(m => m.Severity == StatusSeverity.Error);

    /// <summary>
    /// The most recent message of any severity.
    /// </summary>
    public StatusMessage? LastMessage => Messages.LastOrDefault();
}
