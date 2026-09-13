using System.Net.Http;
using System.Text.Json;
using Composta.Interfaces;
using Composta.Models;

namespace Composta.Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private const string DefaultBaseUrl = "https://api.openweathermap.org/data/2.5";
        private const int ForecastStepsPerDay = 8;
        private const int MaxForecastDays = 5;

        private static readonly HttpClient Client = new HttpClient();

        private string _apiKey;
        private string _baseUrl;

        public OpenWeatherMapService(string apiKey) : this(apiKey, DefaultBaseUrl)
        {
        }

        public OpenWeatherMapService(string apiKey, string baseUrl)
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public string ApiKey
        {
            get { return _apiKey; }
            set { _apiKey = value; }
        }

        public string BaseUrl
        {
            get { return _baseUrl; }
            set { _baseUrl = value.TrimEnd('/'); }
        }

        public WeatherData GetCurrentWeather(string loc)
        {
            string url = $"{_baseUrl}/weather?q={Uri.EscapeDataString(loc)}&appid={GetEscapedKey()}&units=metric";
            using JsonDocument document = JsonDocument.Parse(Fetch(url));
            return ParseWeather(document.RootElement);
        }

        public List<WeatherData> GetForecast(string loc, int days)
        {
            int dayCount = Math.Clamp(days, 1, MaxForecastDays);
            int count = dayCount * ForecastStepsPerDay;
            string url = $"{_baseUrl}/forecast?q={Uri.EscapeDataString(loc)}&appid={GetEscapedKey()}&units=metric&cnt={count}";

            using JsonDocument document = JsonDocument.Parse(Fetch(url));
            List<WeatherData> forecast = new List<WeatherData>();
            int index = 0;

            foreach (JsonElement entry in document.RootElement.GetProperty("list").EnumerateArray())
            {
                if (index % ForecastStepsPerDay == 0)
                {
                    forecast.Add(ParseWeather(entry));
                }
                index++;
            }

            return forecast;
        }

        private string GetEscapedKey()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("API key OpenWeatherMap belum diisi.");
            }

            return Uri.EscapeDataString(_apiKey);
        }

        private static string Fetch(string url)
        {
            return Client.GetStringAsync(url).GetAwaiter().GetResult();
        }

        private static WeatherData ParseWeather(JsonElement element)
        {
            JsonElement main = element.GetProperty("main");
            double temperature = main.GetProperty("temp").GetDouble();
            double humidity = main.GetProperty("humidity").GetDouble();

            double rainfall = 0;
            if (element.TryGetProperty("rain", out JsonElement rain))
            {
                if (rain.TryGetProperty("1h", out JsonElement oneHour))
                {
                    rainfall = oneHour.GetDouble();
                }
                else if (rain.TryGetProperty("3h", out JsonElement threeHours))
                {
                    rainfall = threeHours.GetDouble();
                }
            }

            string condition = string.Empty;
            if (element.TryGetProperty("weather", out JsonElement weather) && weather.GetArrayLength() > 0)
            {
                condition = weather[0].GetProperty("main").GetString() ?? string.Empty;
            }

            DateTime retrievedAt = DateTime.Now;
            if (element.TryGetProperty("dt", out JsonElement timestamp))
            {
                retrievedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp.GetInt64()).LocalDateTime;
            }

            return new WeatherData(temperature, humidity, rainfall, condition, retrievedAt);
        }
    }
}
