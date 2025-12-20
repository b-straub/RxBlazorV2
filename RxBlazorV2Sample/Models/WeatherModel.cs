using R3;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2.MudBlazor.Components;
using RxBlazorV2Sample.Services;

namespace RxBlazorV2Sample.Models;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    // Declare partial constructor with dependencies
    public partial WeatherModel(SettingsModel settings, OpenMeteoApiClient openMeteoClient, StatusModel statusModel);

    private IDisposable? _autoRefreshSubscription;
    
    public bool NotInComponentObservation => Settings.NotInComponentObservation;
    public partial bool IsLoading { get; set; }
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

    // auto-detect usage of Settings.RefreshInterval and Settings.AutoRefresh
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

    // auto-detect usage of Settings.IsDay
    private async Task TestMethodAsync()
    {
        if (Settings.IsDay)
        {
            await Task.Delay(2000);
            Console.WriteLine("Changed to Day!");
        }
    }
    
    private async Task TestMethodCtAsync(CancellationToken ct)
    {
        if (Settings.IsDay)
        {
            await Task.Delay(2000, ct);
            Console.WriteLine("Changed to Day!");
        }
    }

    private async Task LoadWeatherAsync()
    {
        try
        {
            IsLoading = true;

            var forecasts = await OpenMeteoClient.GetWeatherForecastAsync(CurrentLocation);

            if (forecasts.Length > 0)
            {
                Forecasts = forecasts;
                LastRefresh = DateTime.Now;
                UpdateAutoRefreshTimer();
                StatusModel.ClearErrorMessages();
                StatusModel.AddSuccess("New weather forecast loaded");
            }
            else
            {
                StatusModel.AddError("No weather data available");
                Forecasts = null;
            }
        }
        catch (WeatherApiException ex)
        {
            StatusModel.AddError(ex);
            Forecasts = null;
        }
        catch // we do or own error handling overwriting build in command one
        {
            StatusModel.AddError($"Failed to load weather data for: {CurrentLocation}");
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
            StatusModel.AddError("Location cannot be empty");
            return;
        }

        CurrentLocation = newLocation.Trim();
        await LoadWeatherAsync();
    }

    private void SimulateError()
    {
        StatusModel.AddError("Simulated error: Weather service temporarily unavailable");
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