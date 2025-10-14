using System.Text.Json;

namespace RxBlazorV2Sample.Services;

public class LocationService
{
    private readonly HttpClient _httpClient;
    
    public LocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string cityName)
    {
        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count=1";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            
            var result = JsonSerializer.Deserialize<GeocodingResponse>(json, options);
            
            if (result?.Results?.Length > 0)
            {
                var location = result.Results[0];
                return (location.Latitude, location.Longitude);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetLocationNameAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?latitude={latitude:F4}&longitude={longitude:F4}&count=1";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            
            var result = JsonSerializer.Deserialize<GeocodingResponse>(json, options);
            
            if (result?.Results?.Length > 0)
            {
                var location = result.Results[0];
                return $"{location.Name}, {location.Country}";
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}

public class GeocodingResponse
{
    public GeocodingResult[] Results { get; set; } = Array.Empty<GeocodingResult>();
}

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
