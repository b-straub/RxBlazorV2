using RxBlazorV2Sample.Model;

namespace RxBlazorV2Sample.Extensions;

public static class WeatherForecastExtensions
{
    public static async Task<string?> GetWeatherImageUrlAsync(
        this WeatherForecast forecast, 
        WeatherCodeParser parser, bool isDayTime)
    {
        if (forecast.WeatherCode.HasValue)
        {
            return await parser.GetImageUrlAsync(forecast.WeatherCode.Value, isDayTime);
        }
        return null;
    }

    public static async Task<WeatherCondition?> GetWeatherConditionAsync(
        this WeatherForecast forecast, 
        WeatherCodeParser parser, bool isDayTime)
    {
        if (forecast.WeatherCode.HasValue)
        {
            return await parser.GetWeatherConditionAsync(forecast.WeatherCode.Value, isDayTime);
        }
        return null;
    }
}
