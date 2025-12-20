namespace RxBlazorV2.MudBlazor;

/// <summary>
/// Specifies the type of button component for rendering logic.
/// </summary>
public enum ButtonType
{
    DEFAULT,
    ICON,
    FAB
}

/// <summary>
/// Specifies the MudBlazor icon variant style.
/// </summary>
public enum IconVariant
{
    FILLED,
    OUTLINED,
    ROUNDED,
    SHARP,
    TWO_TONE
}

/// <summary>
/// Specifies how status messages are displayed.
/// <para>
/// <b>Valid combinations with StatusMessageMode:</b>
/// <list type="bullet">
/// <item><description><c>Single + SNACKBAR</c>: Snackbar only, each message replaces previous</description></item>
/// <item><description><c>Single + ICON</c>: Icon only, each message replaces previous</description></item>
/// <item><description><c>Single + SNACKBAR_AND_ICON</c>: Both, each message replaces previous</description></item>
/// <item><description><c>Aggregate + ICON</c>: Icon with badge showing count, messages accumulate until cleared</description></item>
/// <item><description><c>Aggregate + SNACKBAR_AND_ICON</c>: Both with aggregated display, messages accumulate until cleared</description></item>
/// <item><description><c>Aggregate + SNACKBAR</c>: Auto-aggregation mode - messages accumulate while snackbar is visible, cleared when snackbar closes</description></item>
/// </list>
/// </para>
/// </summary>
public enum StatusDisplayMode
{
    /// <summary>
    /// Display messages via snackbar only.
    /// With Aggregate mode: messages accumulate while snackbar is visible and are automatically cleared when snackbar closes (timeout or dismissal).
    /// </summary>
    SNACKBAR,

    /// <summary>
    /// Display messages via icon with badge only. Click icon to show snackbar.
    /// Works with both Single and Aggregate message modes.
    /// </summary>
    ICON,

    /// <summary>
    /// Display messages via both snackbar and icon with badge.
    /// Works with both Single and Aggregate message modes. Messages persist until manually cleared via icon click.
    /// </summary>
    SNACKBAR_AND_ICON
}

