using RxBlazorV2.Interface;
using MudBlazor;

namespace RxBlazorV2Sample.Interfaces;

/// <summary>
/// Interface for testing SettingsModel functionality.
/// Contains all public properties from SettingsModel for testing scenarios.
/// </summary>
public interface ISettingsModel : IObservableModel
{
    /// <summary>
    /// Gets or sets the temperature unit (Celsius/Fahrenheit).
    /// </summary>
    string TemperatureUnit { get; set; }
    
    /// <summary>
    /// Gets or sets whether dark mode is enabled.
    /// </summary>
    bool IsDarkMode { get; set; }
    
    /// <summary>
    /// Gets or sets whether auto refresh is enabled.
    /// </summary>
    bool AutoRefresh { get; set; }
    
    /// <summary>
    /// Gets or sets the refresh interval in minutes.
    /// </summary>
    int RefreshInterval { get; set; }
    
    /// <summary>
    /// Gets the current MudBlazor theme based on dark mode setting.
    /// </summary>
    MudTheme CurrentTheme { get; }
    
    /// <summary>
    /// Command to toggle between Celsius and Fahrenheit.
    /// </summary>
    IObservableCommand ToggleUnitCommand { get; }
    
    /// <summary>
    /// Command to toggle between light and dark theme.
    /// </summary>
    IObservableCommand ToggleThemeCommand { get; }
    
    /// <summary>
    /// Command to toggle auto refresh on/off.
    /// </summary>
    IObservableCommand ToggleAutoRefreshCommand { get; }
}