using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Services;

namespace RxBlazorV2Sample.Models;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    // Declare partial constructor with dependencies
    public partial WeatherModel(SettingsModel settings, OpenMeteoApiClient openMeteoClient);

    public bool NotInComponentObservation => Settings.NotInComponentObservation;
    public partial bool IsDay { get; set; }
    public partial bool IsLoading { get; set; }
    public partial string? ErrorMessage { get; set; }
    public partial WeatherForecast[]? Forecasts { get; set; }
    public partial string CurrentLocation { get; set; } = "Karlsruhe";
    public partial int RefreshIntervalMinutes { get; set; } = 5;
    public partial DateTime LastRefresh { get; set; }

    [ObservableCommand(nameof(LoadWeatherAsync), nameof(CanLoadWeather))]
    public partial IObservableCommandAsync LoadWeatherCommand { get; }

    [ObservableCommand(nameof(RefreshAsync), nameof(CanRefresh))]
    [ObservableCommandTrigger(nameof(Settings.IsDay))]
    public partial IObservableCommandAsync RefreshCommand { get; }

    [ObservableCommand(nameof(ChangeLocationAsync), nameof(CanChangeLocation))]
    public partial IObservableCommandAsync<string> ChangeLocationCommand { get; }

    [ObservableCommand(nameof(SimulateError))]
    public partial IObservableCommand SimulateErrorCommand { get; }
    
    private bool CanLoadWeather()
    {
        return !IsLoading && !string.IsNullOrEmpty(CurrentLocation);
    }

    private bool CanRefresh()
    {
        return !IsLoading && Forecasts != null && Settings.AutoRefresh;
    }

    private bool CanChangeLocation()
    {
        return !IsLoading;
    }

    private async Task LoadWeatherAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var forecasts = await OpenMeteoClient.GetWeatherForecastAsync(CurrentLocation, Settings.IsDay);
            
            if (forecasts.Length > 0)
            {
                Forecasts = forecasts;
                LastRefresh = DateTime.Now;
            }
            else
            {
                ErrorMessage = "No weather data available";
                Forecasts = null;
            }
        }
        catch (WeatherApiException ex)
        {
            ErrorMessage = ex.Message;
            Forecasts = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load weather data: {ex.Message}";
            Forecasts = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(2000, ct);
            await LoadWeatherAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //throw;
        }
      
    }

    private async Task ChangeLocationAsync(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
        {
            ErrorMessage = "Location cannot be empty. Use format: latitude,longitude (e.g., 49.0094,8.4044)";
            return;
        }

        var parts = newLocation.Split(',');
        if (parts.Length != 2 || 
            !double.TryParse(parts[0].Trim(), out var lat) || 
            !double.TryParse(parts[1].Trim(), out var lng))
        {
            ErrorMessage = "Invalid location format. Use: latitude,longitude (e.g., 49.0094,8.4044)";
            return;
        }

        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            ErrorMessage = "Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180";
            return;
        }

        CurrentLocation = newLocation.Trim();
        await LoadWeatherAsync();
    }

    private void SimulateError()
    {
        ErrorMessage = "Simulated error: Weather service temporarily unavailable";
        Forecasts = null;
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string? Location { get; set; }
    public int? WeatherCode { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}