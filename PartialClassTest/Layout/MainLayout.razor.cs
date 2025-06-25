using Microsoft.AspNetCore.Components;
using RxBlazorV2Sample.Interfaces;
using RxBlazorV2Sample.Model;

namespace RxBlazorV2Sample.Layout;

public partial class MainLayout : LayoutComponentBase
{
    private readonly SettingsModel _settingsModel;

    bool _drawerOpen = true;

    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}