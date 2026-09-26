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
///
/// Every message belongs to a channel (<see cref="StatusMessage.Channel"/>). Channels are independent:
/// each has its own <see cref="StatusChannelSettings"/> and its own queue, and a message only replaces or
/// cancels messages of its own channel. This lets one status model feed several displays, each serving
/// one channel. Messages a user dismisses from a display are reported through <see cref="Dismissed"/>.
/// </summary>
public abstract class StatusBaseModel : ObservableModel
{
    /// <summary>
    /// One entry of a channel's queue stream: a message to publish once <see cref="Window"/> has elapsed,
    /// or - when <see cref="Message"/> is null - a cancellation that switches the pipeline to an empty stream.
    /// </summary>
    private readonly record struct QueueRequest(StatusMessage? Message, TimeSpan Window);

    /// <summary>
    /// Settings, queue stream and pending message of one channel.
    /// </summary>
    private sealed class ChannelState
    {
        public StatusChannelSettings Settings { get; } = new();

        public Subject<QueueRequest> QueueRequests { get; } = new();

        public StatusMessage? PendingMessage { get; set; }
    }

    private static readonly QueueRequest CancelRequest = new(null, TimeSpan.Zero);

    private readonly Dictionary<string, ChannelState> _channels = [];
    private readonly Subject<IReadOnlyList<StatusMessage>> _dismissed = new();

    /// <summary>
    /// Creates the default channel, so its settings exist before the first message arrives.
    /// </summary>
    protected StatusBaseModel()
    {
        GetChannel(StatusMessage.DefaultChannel);
    }

    /// <summary>
    /// All status messages of all channels, including errors, warnings, info, and success messages.
    /// Changes to this collection trigger component updates via [ObservableComponentTrigger].
    /// </summary>
    [ObservableComponentTrigger]
    public abstract ObservableList<StatusMessage> Messages { get; }

    /// <summary>
    /// Emits the messages a user dismissed - through a display's "Clear All", the close icon of a single
    /// message, or an auto-aggregating snackbar closing - once they have been removed from
    /// <see cref="Messages"/>. Programmatic clears (<c>Clear*</c>) and Single-mode replacement are not
    /// dismissals and do not emit. Each message carries its <see cref="StatusMessage.Channel"/>, so a
    /// subscriber - e.g. a service persisting a dismissed flag - can filter for the channel it owns.
    /// </summary>
    public Observable<IReadOnlyList<StatusMessage>> Dismissed => _dismissed;

    /// <summary>
    /// How error messages of the default channel are accumulated - Aggregate (multiple) or Single (replace).
    /// Use <see cref="GetChannelSettings"/> for other channels.
    /// </summary>
    public StatusMessageMode ErrorMessageMode
    {
        get => DefaultSettings.ErrorMessageMode;
        set => DefaultSettings.ErrorMessageMode = value;
    }

    /// <summary>
    /// How non-error messages (Info, Success, Warning) of the default channel are accumulated.
    /// Use <see cref="GetChannelSettings"/> for other channels.
    /// </summary>
    public StatusMessageMode MessageMessageMode
    {
        get => DefaultSettings.MessageMessageMode;
        set => DefaultSettings.MessageMessageMode = value;
    }

    /// <summary>
    /// How long a queued message of the default channel waits before it is published to
    /// <see cref="Messages"/>. Used by the <c>Queue*</c> methods whenever the caller does not pass an
    /// explicit window. Use <see cref="GetChannelSettings"/> for other channels. Default: 1 second.
    /// <para>
    /// When the window elapses untouched the message is added to <see cref="Messages"/> exactly as an
    /// <c>Add*</c> call would have added it. Inside the window it is dropped - and never displayed - by
    /// any of:
    /// </para>
    /// <list type="bullet">
    /// <item><description>another message of the same channel arriving, queued or immediate, of any
    /// severity;</description></item>
    /// <item><description>a clear that covers it (<see cref="ClearMessages()"/>,
    /// <see cref="ClearNonErrorMessages()"/>, <see cref="ClearMessages(StatusSeverity)"/> for its own
    /// severity, or their channel overloads);</description></item>
    /// <item><description>an explicit <see cref="CancelQueuedMessage()"/>;</description></item>
    /// <item><description>disposal of the model, which unsubscribes the pending delay.</description></item>
    /// </list>
    /// <para>
    /// Only one message per channel is ever queued: queuing a second one replaces the first and restarts
    /// the window. A window of zero or less publishes immediately, which makes the delay a call-site decision.
    /// </para>
    /// </summary>
    public TimeSpan QueueWindow
    {
        get => DefaultSettings.QueueWindow;
        set => DefaultSettings.QueueWindow = value;
    }

    /// <summary>
    /// Time source driving the queue window. Defaults to <see cref="TimeProvider.System"/>; assign a fake
    /// provider in tests to advance the window deterministically instead of waiting for wall-clock time.
    /// </summary>
    public TimeProvider QueueTimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// True while a queued message of any channel is waiting for its window to elapse. Such a message is
    /// not part of <see cref="Messages"/> yet and can still be cancelled.
    /// </summary>
    public bool HasQueuedMessage => _channels.Values.Any(c => c.PendingMessage is not null);

    private StatusChannelSettings DefaultSettings => GetChannel(StatusMessage.DefaultChannel).Settings;

    /// <summary>
    /// Returns the settings of <paramref name="channel"/>, creating the channel on first use.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    public StatusChannelSettings GetChannelSettings(string channel)
    {
        return GetChannel(channel).Settings;
    }

    /// <summary>
    /// Called by command factories when a command throws an exception.
    /// Automatically captures command name and method name as source.
    /// The error is published to the default channel.
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
    /// The error is published to the default channel.
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
    public void AddInfo(string message, string? source = null, string channel = StatusMessage.DefaultChannel)
    {
        AddMessage(message, StatusSeverity.Info, source, channel);
    }

    /// <summary>
    /// Adds a success message.
    /// </summary>
    public void AddSuccess(string message, string? source = null, string channel = StatusMessage.DefaultChannel)
    {
        AddMessage(message, StatusSeverity.Success, source, channel);
    }

    /// <summary>
    /// Adds a warning message.
    /// </summary>
    public void AddWarning(string message, string? source = null, string channel = StatusMessage.DefaultChannel)
    {
        AddMessage(message, StatusSeverity.Warning, source, channel);
    }

    /// <summary>
    /// Adds an error message.
    /// </summary>
    public void AddError(string message, string? source = null, string channel = StatusMessage.DefaultChannel)
    {
        AddMessage(message, StatusSeverity.Error, source, channel);
    }

    /// <summary>
    /// Adds an error message.
    /// </summary>
    public void AddError(Exception ex, string? source = null, string channel = StatusMessage.DefaultChannel)
    {
        AddMessage(ex.Message, StatusSeverity.Error, source, channel);
    }

    /// <summary>
    /// Queues an info message. See <see cref="QueueWindow"/> for the cancellation rules.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="source">Optional source attribution.</param>
    /// <param name="window">Window to wait; the channel's <see cref="StatusChannelSettings.QueueWindow"/> when omitted.</param>
    /// <param name="channel">The channel to publish to.</param>
    public void QueueInfo(string message, string? source = null, TimeSpan? window = null,
        string channel = StatusMessage.DefaultChannel)
    {
        QueueMessage(new StatusMessage(message, StatusSeverity.Info, source, channel), window);
    }

    /// <summary>
    /// Queues a success message. See <see cref="QueueWindow"/> for the cancellation rules.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="source">Optional source attribution.</param>
    /// <param name="window">Window to wait; the channel's <see cref="StatusChannelSettings.QueueWindow"/> when omitted.</param>
    /// <param name="channel">The channel to publish to.</param>
    public void QueueSuccess(string message, string? source = null, TimeSpan? window = null,
        string channel = StatusMessage.DefaultChannel)
    {
        QueueMessage(new StatusMessage(message, StatusSeverity.Success, source, channel), window);
    }

    /// <summary>
    /// Adds a message with the specified severity.
    /// </summary>
    private void AddMessage(string message, StatusSeverity severity, string? source, string channel)
    {
        AddMessage(new StatusMessage(message, severity, source, channel));
    }

    /// <summary>
    /// Adds a prepared message to its channel, applying the channel's accumulation mode for its severity.
    /// Any queued message of the channel still inside its window is cancelled first - the newer message is
    /// the one the user gets to see. Use this overload to supply your own <see cref="StatusMessage.Id"/>,
    /// e.g. the key of a database record the message represents.
    /// </summary>
    /// <param name="message">The message to publish to <see cref="Messages"/>.</param>
    public void AddMessage(StatusMessage message)
    {
        var channel = GetChannel(message.Channel);
        CancelQueuedMessage(channel);

        var mode = message.Severity == StatusSeverity.Error
            ? channel.Settings.ErrorMessageMode
            : channel.Settings.MessageMessageMode;

        if (mode is StatusMessageMode.Single)
        {
            // Clear only messages of the same category (errors vs non-errors) in the same channel
            if (message.Severity == StatusSeverity.Error)
            {
                RemoveMessages(m => m.Channel == message.Channel && m.Severity is StatusSeverity.Error);
            }
            else
            {
                RemoveMessages(m => m.Channel == message.Channel && m.Severity is not StatusSeverity.Error);
            }
        }

        Messages.Add(message);
    }

    /// <summary>
    /// Holds a message back instead of publishing it right away, pushing it into its channel's queue stream
    /// so that the next request switches away from it. See <see cref="QueueWindow"/> for what cancels it and
    /// when it is published.
    /// </summary>
    /// <param name="queued">The message to queue.</param>
    /// <param name="window">Window to wait; the channel's queue window when omitted.</param>
    private void QueueMessage(StatusMessage queued, TimeSpan? window)
    {
        var channel = GetChannel(queued.Channel);
        var delay = window ?? channel.Settings.QueueWindow;

        if (delay <= TimeSpan.Zero)
        {
            AddMessage(queued);
            return;
        }

        // Switch drops whatever was pending; no need to cancel it first.
        channel.PendingMessage = queued;
        channel.QueueRequests.OnNext(new QueueRequest(queued, delay));
    }

    /// <summary>
    /// Drops the queued messages of all channels, if any are waiting, so that they are never displayed.
    /// </summary>
    /// <returns>True when a queued message was dropped.</returns>
    public bool CancelQueuedMessage()
    {
        var cancelled = false;
        foreach (var channel in _channels.Values)
        {
            cancelled |= CancelQueuedMessage(channel);
        }

        return cancelled;
    }

    /// <summary>
    /// Drops the queued message of <paramref name="channel"/>, if one is waiting, so that it is never displayed.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    /// <returns>True when a queued message was dropped.</returns>
    public bool CancelQueuedMessage(string channel)
    {
        return CancelQueuedMessage(GetChannel(channel));
    }

    private bool CancelQueuedMessage(ChannelState channel)
    {
        if (channel.PendingMessage is null)
        {
            return false;
        }

        channel.PendingMessage = null;
        channel.QueueRequests.OnNext(CancelRequest);
        return true;
    }

    /// <summary>
    /// Drops the queued messages whose severity matches <paramref name="severity"/>, in all channels.
    /// </summary>
    /// <param name="severity">The severity to cancel.</param>
    private void CancelQueuedMessages(StatusSeverity severity)
    {
        foreach (var channel in _channels.Values)
        {
            if (channel.PendingMessage is not null && channel.PendingMessage.Severity == severity)
            {
                CancelQueuedMessage(channel);
            }
        }
    }

    /// <summary>
    /// Publishes a channel's queued message once its window has elapsed. Reaching this point means the
    /// pipeline was never switched away from, so no cancellation check is needed here. Clearing the pending
    /// slot first keeps the cancellation inside <see cref="AddMessage(StatusMessage)"/> a no-op, rather than
    /// pushing a request back into the stream whose emission we are currently handling.
    /// </summary>
    private void PublishQueuedMessage(ChannelState channel, StatusMessage queued)
    {
        channel.PendingMessage = null;
        AddMessage(queued);
    }

    /// <summary>
    /// Clears all non-error messages (Info, Success, Warning) of all channels, including queued ones - only
    /// non-error messages can be queued, so this covers every pending message.
    /// </summary>
    public void ClearNonErrorMessages()
    {
        CancelQueuedMessage();
        RemoveMessages(m => m.Severity is not StatusSeverity.Error);
    }

    /// <summary>
    /// Clears the non-error messages of <paramref name="channel"/>, including its queued one.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    public void ClearNonErrorMessages(string channel)
    {
        CancelQueuedMessage(channel);
        RemoveMessages(m => m.Channel == channel && m.Severity is not StatusSeverity.Error);
    }

    /// <summary>
    /// Clears the error messages of all channels. A queued message is never an error, so none is cancelled here.
    /// </summary>
    public void ClearErrorMessages()
    {
        RemoveMessages(m => m.Severity is StatusSeverity.Error);
    }

    /// <summary>
    /// Clears the error messages of <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    public void ClearErrorMessages(string channel)
    {
        RemoveMessages(m => m.Channel == channel && m.Severity is StatusSeverity.Error);
    }

    /// <summary>
    /// Clears all messages of all channels, including queued ones.
    /// </summary>
    public void ClearMessages()
    {
        CancelQueuedMessage();
        Messages.Clear();
    }

    /// <summary>
    /// Clears all messages of <paramref name="channel"/>, including its queued one.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    public void ClearMessages(string channel)
    {
        CancelQueuedMessage(channel);
        RemoveMessages(m => m.Channel == channel);
    }

    /// <summary>
    /// Clears messages with the specified severity in all channels, including queued ones of that severity.
    /// </summary>
    public void ClearMessages(StatusSeverity severity)
    {
        CancelQueuedMessages(severity);
        RemoveMessages(m => m.Severity == severity);
    }

    /// <summary>
    /// Removes <paramref name="messages"/> as dismissed by the user and reports the ones actually removed
    /// through <see cref="Dismissed"/>. Messages no longer in <see cref="Messages"/> are ignored.
    /// </summary>
    /// <param name="messages">The messages the user dismissed.</param>
    public void DismissMessages(IEnumerable<StatusMessage> messages)
    {
        var dismissed = messages.Where(Messages.Remove).ToList();
        if (dismissed.Count > 0)
        {
            _dismissed.OnNext(dismissed);
        }
    }

    /// <summary>
    /// Dismisses all error messages of <paramref name="channel"/>. See <see cref="Dismissed"/>.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    public void DismissErrorMessages(string channel)
    {
        DismissMessages([.. Messages.Where(m => m.Channel == channel && m.Severity is StatusSeverity.Error)]);
    }

    /// <summary>
    /// Dismisses all non-error messages of <paramref name="channel"/> and drops its queued message, which
    /// was never displayed and therefore is not reported as dismissed. See <see cref="Dismissed"/>.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    public void DismissNonErrorMessages(string channel)
    {
        CancelQueuedMessage(channel);
        DismissMessages([.. Messages.Where(m => m.Channel == channel && m.Severity is not StatusSeverity.Error)]);
    }

    private void RemoveMessages(Func<StatusMessage, bool> predicate)
    {
        var toRemove = Messages.Where(predicate).ToList();
        foreach (var msg in toRemove)
        {
            Messages.Remove(msg);
        }
    }

    /// <summary>
    /// Returns the state of <paramref name="name"/>, creating the channel and wiring its queue pipeline on
    /// first use. Every request switches away from the previous one, so a newer message, a cancellation or
    /// disposal unsubscribes the pending delay before it can reach <see cref="Messages"/> - the same
    /// trailing-edge semantics a debounced input stream has, with the message as its payload.
    /// </summary>
    private ChannelState GetChannel(string name)
    {
        if (_channels.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var channel = new ChannelState();
        _channels.Add(name, channel);

        Subscriptions.Add(channel.QueueRequests
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
            .Subscribe(queued => PublishQueuedMessage(channel, queued)));

        return channel;
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
