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
/// </summary>
public enum StatusDisplayMode
{
    SNACKBAR,
    ICON,
    SNACKBAR_AND_ICON
}

/// <summary>
/// Specifies how messages are accumulated.
/// </summary>
public enum StatusMessageMode
{
    AGGREGATE,
    SINGLE
}
