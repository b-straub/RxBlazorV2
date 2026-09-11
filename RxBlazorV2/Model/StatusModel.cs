using ObservableCollections;
using R3;

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
///
/// Messages come in two flavours: the <c>Add*</c> methods publish immediately, <see cref="QueueInfo"/> and
/// <see cref="QueueSuccess"/> hold a message for <see cref="QueueWindow"/> first. A queued message is
/// dropped - never displayed - when another message arrives or the non-error messages are cleared inside
/// that window, which is what keeps a transient "Loading..." from flashing up after the operation it
/// announces has already finished. Only these two severities can be queued: a warning or an error is
/// always worth showing, so it must never be silently dropped.
/// </summary>
public abstract class StatusBaseModel : ObservableModel
{
    /// <summary>
    /// One entry of the queue stream: a message to publish once <see cref="Window"/> has elapsed, or -
    /// when <see cref="Message"/> is null - a cancellation that switches the pipeline to an empty stream.
    /// </summary>
    private readonly record struct QueueRequest(StatusMessage? Message, TimeSpan Window);

    private static readonly QueueRequest CancelRequest = new(null, TimeSpan.Zero);

    private readonly Subject<QueueRequest> _queueRequests = new();
    private StatusMessage? _pendingMessage;

    /// <summary>
    /// Builds the queue pipeline. Every request switches away from the previous one, so a newer message,
    /// a cancellation or disposal unsubscribes the pending delay before it can reach <see cref="Messages"/> -
    /// the same trailing-edge semantics a debounced input stream has, with the message as its payload.
    /// </summary>
    protected StatusBaseModel()
    {
        Subscriptions.Add(_queueRequests
            .Select(request =>
            {
                if (request.Message is not { } message)
                {
                    // Qualified: ObservableModel.Observable shadows the R3 static class inside this type.
                    return R3.Observable.Empty<StatusMessage>();
                }

                return R3.Observable.Timer(request.Window, QueueTimeProvider).Select(_ => message);
            })
            .Switch()
            .Subscribe(PublishQueuedMessage));
    }

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
    /// How long a queued message waits before it is published to <see cref="Messages"/>. Used by the
    /// <c>Queue*</c> methods whenever the caller does not pass an explicit window. Default: 1 second.
    /// <para>
    /// When the window elapses untouched the message is added to <see cref="Messages"/> exactly as an
    /// <c>Add*</c> call would have added it. Inside the window it is dropped - and never displayed - by
    /// any of:
    /// </para>
    /// <list type="bullet">
    /// <item><description>another message arriving, queued or immediate, of any severity;</description></item>
    /// <item><description>a clear that covers it (<see cref="ClearMessages()"/>,
    /// <see cref="ClearNonErrorMessages"/>, or <see cref="ClearMessages(StatusSeverity)"/> for its own
    /// severity);</description></item>
    /// <item><description>an explicit <see cref="CancelQueuedMessage()"/>;</description></item>
    /// <item><description>disposal of the model, which unsubscribes the pending delay.</description></item>
    /// </list>
    /// <para>
    /// Only one message is ever queued: queuing a second one replaces the first and restarts the window.
    /// A window of zero or less publishes immediately, which makes the delay a call-site decision.
    /// </para>
    /// </summary>
    public TimeSpan QueueWindow { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time source driving the queue window. Defaults to <see cref="TimeProvider.System"/>; assign a fake
    /// provider in tests to advance the window deterministically instead of waiting for wall-clock time.
    /// </summary>
    public TimeProvider QueueTimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// True while a queued message is waiting for its window to elapse. Such a message is not part of
    /// <see cref="Messages"/> yet and can still be cancelled.
    /// </summary>
    public bool HasQueuedMessage => _pendingMessage is not null;

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
    /// Called by command factories when a command throws an exception and a per-command error formatter
    /// has produced a user-facing message. Records the formatted text in <see cref="Messages"/> with the
    /// command source attribution; the original exception is accepted for symmetry / future logging hooks.
    /// </summary>
    /// <param name="error">The exception that was thrown (kept for parity with the unformatted overload).</param>
    /// <param name="formattedMessage">The user-facing message produced by the configured formatter.</param>
    /// <param name="commandName">The name of the command property (e.g., "RefreshCommand").</param>
    /// <param name="methodName">The name of the execute method (e.g., "RefreshDataAsync").</param>
    public void HandleError(Exception error, string formattedMessage, string commandName, string methodName)
    {
        _ = error;
        var source = $"{commandName}.{methodName}";
        AddError(formattedMessage, source);
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
    /// Queues an info message. See <see cref="QueueWindow"/> for the cancellation rules.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="source">Optional source attribution.</param>
    /// <param name="window">Window to wait; <see cref="QueueWindow"/> when omitted.</param>
    public void QueueInfo(string message, string? source = null, TimeSpan? window = null)
    {
        QueueMessage(message, StatusSeverity.Info, source, window);
    }

    /// <summary>
    /// Queues a success message. See <see cref="QueueWindow"/> for the cancellation rules.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="source">Optional source attribution.</param>
    /// <param name="window">Window to wait; <see cref="QueueWindow"/> when omitted.</param>
    public void QueueSuccess(string message, string? source = null, TimeSpan? window = null)
    {
        QueueMessage(message, StatusSeverity.Success, source, window);
    }


    /// <summary>
    /// Adds a message with the specified severity.
    /// </summary>
    private void AddMessage(string message, StatusSeverity severity, string? source)
    {
        AddMessage(new StatusMessage(message, severity, source));
    }

    /// <summary>
    /// Adds a prepared message, applying the accumulation mode of its severity. Any queued message still
    /// inside its window is cancelled first - the newer message is the one the user gets to see.
    /// </summary>
    /// <param name="message">The message to publish to <see cref="Messages"/>.</param>
    private void AddMessage(StatusMessage message)
    {
        CancelQueuedMessage();

        var mode = message.Severity == StatusSeverity.Error ? ErrorMessageMode : MessageMessageMode;

        if (mode is StatusMessageMode.Single)
        {
            // Clear only messages of the same category (errors vs non-errors)
            if (message.Severity == StatusSeverity.Error)
            {
                ClearMessages(StatusSeverity.Error);
            }
            else
            {
                ClearNonErrorMessages();
            }
        }

        Messages.Add(message);
    }

    /// <summary>
    /// Holds a message back for <paramref name="window"/> instead of publishing it right away, pushing it
    /// into the queue stream so that the next request switches away from it. See <see cref="QueueWindow"/>
    /// for what cancels it and when it is published.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="severity">The severity of the message.</param>
    /// <param name="source">Optional source attribution.</param>
    /// <param name="window">Window to wait; <see cref="QueueWindow"/> when omitted.</param>
    private void QueueMessage(string message, StatusSeverity severity, string? source, TimeSpan? window)
    {
        var delay = window ?? QueueWindow;
        var queued = new StatusMessage(message, severity, source);

        if (delay <= TimeSpan.Zero)
        {
            AddMessage(queued);
            return;
        }

        // Switch drops whatever was pending; no need to cancel it first.
        _pendingMessage = queued;
        _queueRequests.OnNext(new QueueRequest(queued, delay));
    }

    /// <summary>
    /// Drops the queued message, if one is waiting, so that it is never displayed.
    /// </summary>
    /// <returns>True when a queued message was dropped.</returns>
    public bool CancelQueuedMessage()
    {
        if (_pendingMessage is null)
        {
            return false;
        }

        _pendingMessage = null;
        _queueRequests.OnNext(CancelRequest);
        return true;
    }

    /// <summary>
    /// Drops the queued message when its severity matches <paramref name="severity"/>.
    /// </summary>
    /// <param name="severity">The severity to cancel.</param>
    /// <returns>True when a queued message was dropped.</returns>
    private bool CancelQueuedMessage(StatusSeverity severity)
    {
        if (_pendingMessage is null || _pendingMessage.Severity != severity)
        {
            return false;
        }

        return CancelQueuedMessage();
    }

    /// <summary>
    /// Publishes the queued message once its window has elapsed. Reaching this point means the pipeline
    /// was never switched away from, so no cancellation check is needed here. Clearing the pending slot
    /// first keeps the cancellation inside <see cref="AddMessage(StatusMessage)"/> a no-op, rather than
    /// pushing a request back into the stream whose emission we are currently handling.
    /// </summary>
    private void PublishQueuedMessage(StatusMessage queued)
    {
        _pendingMessage = null;
        AddMessage(queued);
    }

    /// <summary>
    /// Clears all non-error messages (Info, Success, Warning), including a queued one - only non-error
    /// messages can be queued, so this covers every pending message.
    /// </summary>
    public void ClearNonErrorMessages()
    {
        CancelQueuedMessage();

        var toRemove = Messages.Where(m => m.Severity is not StatusSeverity.Error).ToList();
        foreach (var msg in toRemove)
        {
            Messages.Remove(msg);
        }
    }
    
    /// <summary>
    /// Clears all error messages (Error). A queued message is never an error, so none is cancelled here.
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
    /// Clears all messages, including a queued one.
    /// </summary>
    public void ClearMessages()
    {
        CancelQueuedMessage();
        Messages.Clear();
    }

    /// <summary>
    /// Clears messages with the specified severity, including a queued one of that severity.
    /// </summary>
    public void ClearMessages(StatusSeverity severity)
    {
        CancelQueuedMessage(severity);

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
