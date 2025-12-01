using R3;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Services;
using RxBlazorVSSampleComponents.ErrorManager;

namespace RxBlazorV2Sample.Models;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    // Declare partial constructor with dependencies
    public partial WeatherModel(SettingsModel settings, OpenMeteoApiClient openMeteoClient);

    private IDisposable? _autoRefreshSubscription;

    protected override void OnContextReady()
    {
        base.OnContextReady();
        Settings.OnAutoRefreshChanged(UpdateAutoRefreshTimer);
        Settings.OnRefreshIntervalChanged(UpdateAutoRefreshTimer);
    }

    public bool NotInComponentObservation => Settings.NotInComponentObservation;
    public partial bool IsLoading { get; set; }
    public partial string? ErrorMessage { get; set; }
    public partial WeatherForecast[]? Forecasts { get; set; }
    public partial string CurrentLocation { get; set; } = "Berlin";
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
        return !IsLoading && Forecasts is not null && Settings.AutoRefresh;
    }

    private bool CanChangeLocation()
    {
        return !IsLoading;
    }

    private void UpdateAutoRefreshTimer()
    {
        _autoRefreshSubscription?.Dispose();
        _autoRefreshSubscription = null;

        if (Settings.AutoRefresh && Forecasts is not null)
        {
            _autoRefreshSubscription = R3.Observable
                .Interval(TimeSpan.FromMinutes(Settings.RefreshInterval))
                .Subscribe(_ =>
                {
                    if (CanRefresh())
                    {
                        RefreshCommand.ExecuteAsync();
                    }
                });
        }
    }

    private async Task LoadWeatherAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var forecasts = await OpenMeteoClient.GetWeatherForecastAsync(CurrentLocation);

            if (forecasts.Length > 0)
            {
                Forecasts = forecasts;
                LastRefresh = DateTime.Now;
                UpdateAutoRefreshTimer();
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
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoRefreshSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }

    private async Task ChangeLocationAsync(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
        {
            ErrorMessage = "Location cannot be empty";
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