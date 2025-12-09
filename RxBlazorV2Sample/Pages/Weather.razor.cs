using MudBlazor;
using RxBlazorV2Sample.Models;

// Extend the generated WeatherComponent with custom view logic
namespace RxBlazorV2Sample.Pages;

public partial class Weather : WeatherModelComponent
{
    protected override async Task OnContextReadyAsync()
    {
        // Load initial weather data
        await Model.LoadWeatherCommand.ExecuteAsync();
    }
    protected async Task RefreshAsync()
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

    protected Color GetTemperatureColor(int temperatureC)
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

    protected string GetWeatherIcon(string? summary)
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