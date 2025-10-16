using MudBlazor;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Interfaces;

namespace RxBlazorV2Sample.Models;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class SettingsModel : ObservableModel, ISettingsModel
{
    public partial string TemperatureUnit { get; set; } = "Celsius";
    public partial bool IsDay { get; set; } = false;
    public partial bool AutoRefresh { get; set; } = true;
    public partial int RefreshInterval { get; set; } = 5;
    public partial bool NotInComponentObservation { get; set; }

    // Computed property for MudBlazor theme
    public MudTheme CurrentTheme => IsDay ? DarkTheme : LightTheme;

    private static readonly MudTheme LightTheme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Default,
        }
    };

    private static readonly MudTheme DarkTheme = new MudTheme()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = Colors.Blue.Lighten1,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Gray.Darken4,
            Background = Colors.Gray.Darken4,
            DrawerBackground = Colors.Gray.Darken3,
            Surface = Colors.Gray.Darken3,
        }
    };
    
    [ObservableCommand(nameof(ToggleUnit))]
    public partial IObservableCommand ToggleUnitCommand { get; }
    
    [ObservableCommand(nameof(ToggleTheme))]
    public partial IObservableCommand ToggleThemeCommand { get; }
    
    [ObservableCommand(nameof(ToggleAutoRefresh))]
    public partial IObservableCommand ToggleAutoRefreshCommand { get; }
    
    private void ToggleUnit()
    {
        TemperatureUnit = TemperatureUnit == "Celsius" ? "Fahrenheit" : "Celsius";
    }
    
    private void ToggleTheme()
    {
        IsDay = !IsDay;
    }
    
    private void ToggleAutoRefresh()
    {
        AutoRefresh = !AutoRefresh;
    }
}