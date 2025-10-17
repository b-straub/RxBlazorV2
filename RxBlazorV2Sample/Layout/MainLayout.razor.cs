using Microsoft.AspNetCore.Components;
using R3;

namespace RxBlazorV2Sample.Layout;

public partial class MainLayout : LayoutComponentBase
{
    private bool _drawerOpen = true;
    
    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}

public partial class MainLayout
{
    [Inject]
    public required RxBlazorVSSampleComponents.Settings.SettingsModel Settings { get; init; }

    protected override void OnInitialized()
    {
        Settings.Subscriptions.Add(Settings.Observable
            .Where(p => p.Contains("IsDay"))
            .Subscribe(_ => InvokeAsync(StateHasChanged)));
    }
}