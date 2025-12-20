namespace RxBlazorV2.Model;

/// <summary>
/// Status message severity levels.
/// </summary>
public enum StatusSeverity
{
    Info,
    Success,
    Warning,
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
/// Represents a status message with severity level and source context.
/// </summary>
/// <param name="Message">The status message text.</param>
/// <param name="Severity">The severity level of the message.</param>
/// <param name="Source">The source of the message (e.g., "RefreshCommand.RefreshDataAsync").</param>
public record StatusMessage(
    string Message,
    StatusSeverity Severity,
    string? Source = null)
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
