using Microsoft.AspNetCore.Components;
using RxBlazorV2.Component;
using RxBlazorV2Sample.Interfaces;

namespace RxBlazorV2Sample.Layout;

public partial class MainLayout : ObservableLayoutComponentBase
{
    [Inject]
    public required ISettingsModel Settings { get; init; }

    private bool _drawerOpen = true;

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}