namespace RxBlazorV2.Model;

/// <summary>
/// Status message severity levels.
/// </summary>
public enum StatusSeverity
{
    /// <summary>Informational message.</summary>
    Info,
    /// <summary>Success message.</summary>
    Success,
    /// <summary>Warning message.</summary>
    Warning,
    /// <summary>Error message.</summary>
    Error
}

/// <summary>
/// Specifies how messages are accumulated.
/// <para>
/// <b>Valid combinations with StatusDisplayMode:</b>
/// <list type="bullet">
/// <item><description><c>Single</c>: Works with all display modes (SNACKBAR, ICON, SNACKBAR_AND_ICON)</description></item>
/// <item><description><c>Aggregate</c>: Requires ICON or SNACKBAR_AND_ICON display mode. SNACKBAR alone is auto-upgraded.</description></item>
/// </list>
/// </para>
/// </summary>
public enum StatusMessageMode
{
    /// <summary>
    /// Multiple messages can accumulate in the list.
    /// Requires ICON or SNACKBAR_AND_ICON display mode for proper aggregated display.
    /// Using SNACKBAR alone with Aggregate mode automatically upgrades to SNACKBAR_AND_ICON.
    /// </summary>
    Aggregate,

    /// <summary>
    /// Only one message at a time - new message clears previous.
    /// Works with all display modes (SNACKBAR, ICON, SNACKBAR_AND_ICON).
    /// </summary>
    Single
}

/// <summary>
/// Accumulation and queue settings of one status channel. Every channel of a
/// <see cref="StatusBaseModel"/> has its own settings, so several displays bound to the same model do not
/// overwrite each other's modes.
/// </summary>
public sealed class StatusChannelSettings
{
    /// <summary>
    /// How error messages of the channel are accumulated - Aggregate (multiple) or Single (replace).
    /// </summary>
    public StatusMessageMode ErrorMessageMode { get; set; } = StatusMessageMode.Aggregate;

    /// <summary>
    /// How non-error messages (Info, Success, Warning) of the channel are accumulated.
    /// </summary>
    public StatusMessageMode MessageMessageMode { get; set; } = StatusMessageMode.Aggregate;

    /// <summary>
    /// How long a queued message of the channel waits before it is published. Default: 1 second.
    /// </summary>
    public TimeSpan QueueWindow { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Represents a status message with severity level and source context.
/// </summary>
/// <param name="Message">The status message text.</param>
/// <param name="Severity">The severity level of the message.</param>
/// <param name="Source">The source of the message (e.g., "RefreshCommand.RefreshDataAsync").</param>
/// <param name="Channel">The channel the message belongs to. A status display serves exactly one channel,
/// which lets one status model feed several displays - e.g. errors and system notifications on
/// <see cref="DefaultChannel"/>, database notifications on a custom channel.</param>
public record StatusMessage(
    string Message,
    StatusSeverity Severity,
    string? Source = null,
    string Channel = StatusMessage.DefaultChannel)
{
    /// <summary>
    /// The channel used when none is specified. Command errors are always published to this channel.
    /// </summary>
    public const string DefaultChannel = "default";

    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
