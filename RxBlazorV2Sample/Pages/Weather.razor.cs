using MudBlazor;
using RxBlazorV2.Component;
using RxBlazorV2Sample.Interfaces;
using WeatherModel = RxBlazorV2Sample.Models.WeatherModel;

namespace RxBlazorV2Sample.Pages;

public partial class Weather : ObservableComponent<WeatherModel>
{
    private readonly ISettingsModel _settings;
    
    protected override async Task OnInitializedAsync()
    {
        // Load initial weather data
        await Model.LoadWeatherCommand.ExecuteAsync();
    }

    private async Task RefreshAsync()
    {
        if (Model.RefreshCommand.Executing)
        {
            Model.RefreshCommand.Cancel();
        }
        else
        {
            await Model.RefreshCommand.ExecuteAsync();
        }
    }
    private Color GetTemperatureColor(int temperatureC)
    {
        return temperatureC switch
        {
            < 0 => Color.Info,     // Freezing - Blue
            < 10 => Color.Primary, // Cold - Primary Blue
            < 20 => Color.Success, // Mild - Green
            < 30 => Color.Warning, // Warm - Orange
            _ => Color.Error       // Hot - Red
        };
    }
    
    private string GetWeatherIcon(string? summary)
    {
        return summary?.ToLower() switch
        {
            var s when s?.Contains("sunny") == true => "☀️",
            var s when s?.Contains("cloudy") == true => "☁️",
            var s when s?.Contains("rain") == true => "🌧️",
            var s when s?.Contains("snow") == true => "❄️",
            var s when s?.Contains("storm") == true => "⛈️",
            var s when s?.Contains("fog") == true => "🌫️",
            var s when s?.Contains("wind") == true => "💨",
            _ => "🌤️"
        };
    }
}