using System.Text.Json;
using System.Text.Json.Serialization;

namespace RxBlazorV2Sample.Models;

public class WeatherCodeParser
{
    private readonly HttpClient _httpClient;
    private Dictionary<int, WeatherCodeInfo>? _weatherCodes;

    public WeatherCodeParser(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<int, WeatherCodeInfo>?> LoadWeatherCodesAsync()
    {
        if (_weatherCodes == null)
        {
            try
            {
                var json = await _httpClient.GetStringAsync("data/weatherCodes.json");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var rawData = JsonSerializer.Deserialize<Dictionary<string, WeatherCodeInfo>>(json, options);
                
                if (rawData != null)
                {
                    _weatherCodes = rawData.ToDictionary(
                        kvp => int.Parse(kvp.Key), 
                        kvp => kvp.Value
                    );
                }
            }
            catch (Exception ex)
            {
                // Log exception or handle as needed
                throw new Exception($"Failed to load weather codes: {ex.Message}", ex);
            }
        }

        return _weatherCodes;
    }

    public async Task<WeatherCodeInfo?> GetWeatherCodeInfoAsync(int code)
    {
        var weatherCodes = await LoadWeatherCodesAsync();
        return weatherCodes?.GetValueOrDefault(code);
    }

    public async Task<WeatherCondition?> GetWeatherConditionAsync(int code, bool isDayTime = true)
    {
        var weatherCodeInfo = await GetWeatherCodeInfoAsync(code);
        return isDayTime ? weatherCodeInfo?.Day : weatherCodeInfo?.Night;
    }

    public async Task<string?> GetDescriptionAsync(int code, bool isDayTime = true)
    {
        var condition = await GetWeatherConditionAsync(code, isDayTime);
        return condition?.Description;
    }

    public async Task<string?> GetImageUrlAsync(int code, bool isDayTime = true)
    {
        var condition = await GetWeatherConditionAsync(code, isDayTime);
        return condition?.Image;
    }

    public void ClearCache()
    {
        _weatherCodes = null;
    }
}

public class WeatherCodeInfo
{
    [JsonPropertyName("day")]
    public WeatherCondition Day { get; set; } = new();

    [JsonPropertyName("night")]
    public WeatherCondition Night { get; set; } = new();
}

public class WeatherCondition
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;
}