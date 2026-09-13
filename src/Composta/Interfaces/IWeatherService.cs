using Composta.Models;

namespace Composta.Interfaces
{
    public interface IWeatherService
    {
        WeatherData GetCurrentWeather(string loc);

        List<WeatherData> GetForecast(string loc, int days);
    }
}
