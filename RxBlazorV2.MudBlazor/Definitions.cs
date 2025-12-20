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
/// <item><description><c>Aggregate + ICON</c>: Icon with badge showing count, messages accumulate</description></item>
/// <item><description><c>Aggregate + SNACKBAR_AND_ICON</c>: Both with aggregated display, messages accumulate</description></item>
/// <item><description><c>Aggregate + SNACKBAR</c>: <b>Not allowed</b> - automatically upgraded to SNACKBAR_AND_ICON</description></item>
/// </list>
/// </para>
/// </summary>
public enum StatusDisplayMode
{
    /// <summary>
    /// Display messages via snackbar only. Only valid with <see cref="RxBlazorV2.Model.StatusMessageMode.Single"/>.
    /// When used with Aggregate mode, automatically upgrades to <see cref="SNACKBAR_AND_ICON"/>.
    /// </summary>
    SNACKBAR,

    /// <summary>
    /// Display messages via icon with badge only. Click icon to show snackbar.
    /// Works with both Single and Aggregate message modes.
    /// </summary>
    ICON,

    /// <summary>
    /// Display messages via both snackbar and icon with badge.
    /// Works with both Single and Aggregate message modes. Required for Aggregate mode with snackbar.
    /// </summary>
    SNACKBAR_AND_ICON
}

