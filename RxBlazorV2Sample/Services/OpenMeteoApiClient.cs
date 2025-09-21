using System.Text.Json;
using System.Text.Json.Serialization;
using RxBlazorV2Sample.Models;

namespace RxBlazorV2Sample.Services;

public class OpenMeteoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WeatherCodeParser _weatherCodeParser;
    private readonly LocationService _locationService;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public OpenMeteoApiClient(HttpClient httpClient, WeatherCodeParser weatherCodeParser,
        LocationService locationService)
    {
        _httpClient = httpClient;
        _weatherCodeParser = weatherCodeParser;
        _locationService = locationService;
    }

    public async Task<OpenMeteoResponse?> GetWeatherAsync(double latitude, double longitude, int forecastDays = 7)
    {
        try
        {
            var url = BuildApiUrl(latitude, longitude, forecastDays);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            return JsonSerializer.Deserialize<OpenMeteoResponse>(jsonString, options);
        }
        catch (Exception ex)
        {
            throw new WeatherApiException($"Failed to fetch weather data for coordinates ({latitude}, {longitude})",
                ex);
        }
    }

    public async Task<WeatherForecast[]> GetWeatherForecastAsync(double latitude, double longitude, bool isDayTime,
        string? locationName = null, int forecastDays = 7)
    {
        var response = await GetWeatherAsync(latitude, longitude, forecastDays);
        if (response == null) return Array.Empty<WeatherForecast>();

        return await ConvertToWeatherForecastAsync(response, locationName ?? $"{latitude:F4}, {longitude:F4}", isDayTime);
    }

    public async Task<WeatherForecast[]> GetWeatherForecastAsync(string location, bool isDayTime, int forecastDays = 7)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be null or empty", nameof(location));
        }

        var coordinates = await _locationService.GetCoordinatesAsync(location);

        if (!coordinates.HasValue)
        {
            throw new ArgumentException("Location not found", nameof(location));
        }

        return await GetWeatherForecastAsync(coordinates.Value.Latitude, coordinates.Value.Longitude, isDayTime, location,
            forecastDays);
    }

    private string BuildApiUrl(double latitude, double longitude, int forecastDays)
    {
        var latitudeF4 = latitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        var longitudeF4 = longitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

        return $"{BaseUrl}?" +
               $"latitude={latitudeF4}&" +
               $"longitude={longitudeF4}&" +
               $"daily=temperature_2m_max,weather_code&" +
               $"current=temperature_2m&" +
               $"timezone=auto&" +
               $"forecast_days={forecastDays}";
    }

    private async Task<WeatherForecast[]> ConvertToWeatherForecastAsync(OpenMeteoResponse response, string location, bool isDayTime)
    {
        var forecasts = new List<WeatherForecast>();

        if (response.Current != null && response.Daily?.Time?.Length > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var todayIndex = Array.FindIndex(response.Daily.Time, t => DateOnly.Parse(t) == today);

            if (todayIndex >= 0 && todayIndex < response.Daily.TemperatureMax.Length)
            {
                var weatherCode = response.Daily.WeatherCode[todayIndex];
                var description = await _weatherCodeParser.GetDescriptionAsync(weatherCode, isDayTime) ??
                                  GetFallbackWeatherDescription(weatherCode);

                forecasts.Add(new WeatherForecast
                {
                    Date = today,
                    Location = location,
                    TemperatureC = (int)Math.Round(response.Current.Temperature),
                    Summary = description,
                    WeatherCode = weatherCode
                });
            }
        }

        if (response.Daily != null)
        {
            var startIndex = forecasts.Count > 0 ? 1 : 0;

            for (int i = startIndex;
                 i < Math.Min(response.Daily.Time.Length, response.Daily.TemperatureMax.Length);
                 i++)
            {
                var weatherCode = response.Daily.WeatherCode[i];
                var description = await _weatherCodeParser.GetDescriptionAsync(weatherCode, true) ??
                                  GetFallbackWeatherDescription(weatherCode);

                forecasts.Add(new WeatherForecast
                {
                    Date = DateOnly.Parse(response.Daily.Time[i]),
                    Location = location,
                    TemperatureC = (int)Math.Round(response.Daily.TemperatureMax[i]),
                    Summary = description,
                    WeatherCode = weatherCode
                });
            }
        }

        return forecasts.ToArray();
    }
    
    private string GetFallbackWeatherDescription(int weatherCode)
    {
        return weatherCode switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 => "Light drizzle",
            53 => "Moderate drizzle",
            55 => "Dense drizzle",
            56 => "Light freezing drizzle",
            57 => "Dense freezing drizzle",
            61 => "Slight rain",
            63 => "Moderate rain",
            65 => "Heavy rain",
            66 => "Light freezing rain",
            67 => "Heavy freezing rain",
            71 => "Slight snow fall",
            73 => "Moderate snow fall",
            75 => "Heavy snow fall",
            77 => "Snow grains",
            80 => "Slight rain showers",
            81 => "Moderate rain showers",
            82 => "Violent rain showers",
            85 => "Slight snow showers",
            86 => "Heavy snow showers",
            95 => "Thunderstorm",
            96 => "Thunderstorm with slight hail",
            99 => "Thunderstorm with heavy hail",
            _ => "Unknown weather condition"
        };
    }
}

public class OpenMeteoResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [JsonPropertyName("generationtime_ms")]
    public double GenerationTimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_abbreviation")]
    public string TimezoneAbbreviation { get; set; } = string.Empty;

    public double Elevation { get; set; }

    [JsonPropertyName("current_units")]
    public CurrentUnits? CurrentUnits { get; set; }

    public Current? Current { get; set; }

    [JsonPropertyName("daily_units")]
    public DailyUnits? DailyUnits { get; set; }

    public Daily? Daily { get; set; }
}

public class CurrentUnits
{
    public string Time { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public string Temperature { get; set; } = string.Empty;
}

public class Current
{
    public string Time { get; set; } = string.Empty;
    public int Interval { get; set; }

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }
}

public class DailyUnits
{
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m_max")]
    public string TemperatureMax { get; set; } = string.Empty;

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; } = string.Empty;
}

public class Daily
{
    public string[] Time { get; set; } = Array.Empty<string>();

    [JsonPropertyName("temperature_2m_max")]
    public double[] TemperatureMax { get; set; } = Array.Empty<double>();

    [JsonPropertyName("weather_code")]
    public int[] WeatherCode { get; set; } = Array.Empty<int>();
}

public class WeatherApiException : Exception
{
    public WeatherApiException(string message) : base(message)
    {
    }

    public WeatherApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}