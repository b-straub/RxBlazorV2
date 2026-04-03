using MudBlazor;

namespace RxBlazorV2.MudBlazor.Internal;

/// <summary>
/// Extension methods for resolving MudBlazor icons based on the selected icon variant.
/// </summary>
internal static class IconExtensions
{
    /// <summary>
    /// Returns the cancel icon matching the specified variant style.
    /// </summary>
    public static string GetCancelIcon(this IconVariant? iconVariant)
    {
        return iconVariant switch
        {
            IconVariant.FILLED => Icons.Material.Filled.Cancel,
            IconVariant.OUTLINED => Icons.Material.Outlined.Cancel,
            IconVariant.SHARP => Icons.Material.Sharp.Cancel,
            IconVariant.ROUNDED => Icons.Material.Rounded.Cancel,
            IconVariant.TWO_TONE => Icons.Material.TwoTone.Cancel,
            _ => Icons.Material.Outlined.Cancel
        };
    }

    /// <summary>
    /// Returns the progress/refresh icon matching the specified variant style.
    /// </summary>
    public static string GetProgressIcon(this IconVariant? iconVariant)
    {
        return iconVariant switch
        {
            IconVariant.FILLED => Icons.Material.Filled.Refresh,
            IconVariant.OUTLINED => Icons.Material.Outlined.Refresh,
            IconVariant.SHARP => Icons.Material.Sharp.Refresh,
            IconVariant.ROUNDED => Icons.Material.Rounded.Refresh,
            IconVariant.TWO_TONE => Icons.Material.TwoTone.Refresh,
            _ => Icons.Material.Filled.Refresh
        };
    }
}
