using Composta.Interfaces;
using Composta.Models;

namespace Composta.Services
{
    // Data cuaca contoh, dipakai selama API key OpenWeatherMap/Azure Maps belum diatur
    public class OfflineWeatherService : IWeatherService
    {
        public WeatherData GetCurrentWeather(string loc)
        {
            return new WeatherData(29.5, 78, 0, "Cerah Berawan", DateTime.Now);
        }

        public List<WeatherData> GetForecast(string loc, int days)
        {
            List<WeatherData> forecast = new List<WeatherData>();
            for (int i = 1; i <= days; i++)
            {
                forecast.Add(new WeatherData(29 + i % 3, 75 + i * 2, i % 2 == 0 ? 4 : 0,
                    i % 2 == 0 ? "Hujan Ringan" : "Cerah Berawan", DateTime.Now.AddDays(i)));
            }
            return forecast;
        }
    }
}
