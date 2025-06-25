using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using System.Net.Http.Json;
using RxBlazorV2Sample.Interfaces;

namespace RxBlazorV2Sample.Model;

[ObservableModelReference<ISettingsModel>]
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    private readonly HttpClient _httpClient;

    public partial bool IsLoading { get; set; }
    public partial WeatherForecast[]? Forecasts { get; set; }
    public partial string CurrentLocation { get; set; } = "San Francisco";
    public partial int RefreshIntervalMinutes { get; set; } = 5;
    public partial DateTime LastRefresh { get; set; }

    [ObservableCommand(nameof(LoadWeatherAsync), nameof(CanLoadWeather))]
    public partial IObservableCommandAsync<bool> LoadWeatherCommand { get; }

    [ObservableCommand(nameof(RefreshAsync), nameof(CanRefresh))]
    [ObservableCommandTrigger(nameof(ISettingsModel.IsDarkMode), nameof(CanTrigger))]
    public partial IObservableCommandAsync RefreshCommand { get; }

    [ObservableCommand(nameof(ChangeLocationAsync), nameof(CanChangeLocation))]
    public partial IObservableCommandAsync<string> ChangeLocationCommand { get; }

    [ObservableCommand(nameof(LoadWeatherAsync))]
    public partial IObservableCommandAsync<bool> SimulateErrorCommand { get; }

    private bool CanTrigger()
    {
        return true;
    }
    
    private bool CanLoadWeather()
    {
        return !IsLoading && !string.IsNullOrEmpty(CurrentLocation);
    }

    private bool CanRefresh()
    {
        return !IsLoading && Forecasts != null && SettingsModel.AutoRefresh;
    }

    private bool CanChangeLocation()
    {
        return !IsLoading;
    }

    private async Task LoadWeatherAsync(bool simulateError = false)
    {
        try
        {
            IsLoading = true;

            // Simulate API call delay
            await Task.Delay(1500);

            if (simulateError)
            {
                throw new InvalidOperationException("Simulated error");
            }
            
            // Load from sample data
            var weather = await _httpClient.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json");

            // Simulate location-specific data by modifying temperatures
            if (weather != null)
            {
                var locationOffset = CurrentLocation.GetHashCode() % 20 - 10; // -10 to +10 variation
                foreach (var forecast in weather)
                {
                    forecast.TemperatureC += locationOffset;
                    forecast.Location = CurrentLocation;
                }
            }

            Forecasts = weather;
            LastRefresh = DateTime.Now;
        }
        catch (Exception ex)
        {
            Forecasts = null;
            throw new InvalidOperationException($"Failed to load weather data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        await LoadWeatherAsync();
    }

    private async Task ChangeLocationAsync(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
        {
            throw new InvalidOperationException($"Location cannot be empty");
        }

        CurrentLocation = newLocation.Trim();
        await LoadWeatherAsync();
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string? Location { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}