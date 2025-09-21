using Microsoft.AspNetCore.Components;
using SettingsModel = RxBlazorV2Sample.Models.SettingsModel;

namespace RxBlazorV2Sample.Layout;

public partial class MainLayout : LayoutComponentBase
{
    private readonly SettingsModel _settingsModel;

    private bool _drawerOpen = true;

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}