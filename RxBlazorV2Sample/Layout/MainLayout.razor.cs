using RxBlazorV2Sample.Models;

namespace RxBlazorV2Sample.Layout;

public partial class MainLayout : SettingsModelComponent
{
    private bool _drawerOpen = true;
    
    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}