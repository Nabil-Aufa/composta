using System.Net.Http;
using System.Text.Json;
using Composta.Interfaces;
using Composta.Models;

namespace Composta.Services
{
    public class AzureMapsWeatherService : IWeatherService
    {
        private const string BaseUrl = "https://atlas.microsoft.com/weather";
        private const string ApiVersion = "1.1";
        private static readonly int[] SupportedDurations = { 1, 5, 10, 15, 25, 45 };

        private static readonly HttpClient Client = new HttpClient();

        private string _subscriptionKey;

        public AzureMapsWeatherService(string subscriptionKey)
        {
            _subscriptionKey = subscriptionKey;
        }

        public string SubscriptionKey
        {
            get { return _subscriptionKey; }
            set { _subscriptionKey = value; }
        }

        public WeatherData GetCurrentWeather(string loc)
        {
            string url = $"{BaseUrl}/currentConditions/json?api-version={ApiVersion}&query={Uri.EscapeDataString(loc)}&subscription-key={GetEscapedKey()}";
            using JsonDocument document = JsonDocument.Parse(Fetch(url));

            JsonElement results = document.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                throw new InvalidOperationException($"Data cuaca untuk lokasi {loc} tidak ditemukan.");
            }

            JsonElement current = results[0];
            return new WeatherData(
                ReadDouble(current, "temperature", "value"),
                ReadDouble(current, "relativeHumidity"),
                ReadDouble(current, "precipitationSummary", "pastHour", "value"),
                ReadString(current, "phrase"),
                ReadDate(current, "dateTime"));
        }

        public List<WeatherData> GetForecast(string loc, int days)
        {
            int dayCount = Math.Clamp(days, 1, SupportedDurations[^1]);
            int duration = SupportedDurations.First(d => d >= dayCount);
            string url = $"{BaseUrl}/forecast/daily/json?api-version={ApiVersion}&query={Uri.EscapeDataString(loc)}&duration={duration}&subscription-key={GetEscapedKey()}";

            using JsonDocument document = JsonDocument.Parse(Fetch(url));
            List<WeatherData> forecast = new List<WeatherData>();

            foreach (JsonElement day in document.RootElement.GetProperty("forecasts").EnumerateArray().Take(dayCount))
            {
                double maximum = ReadDouble(day, "temperature", "maximum", "value");
                double minimum = ReadDouble(day, "temperature", "minimum", "value");

                forecast.Add(new WeatherData(
                    (maximum + minimum) / 2,
                    ReadDouble(day, "day", "relativeHumidity"),
                    ReadDouble(day, "day", "totalLiquid", "value"),
                    ReadString(day, "day", "iconPhrase"),
                    ReadDate(day, "date")));
            }

            return forecast;
        }

        private string GetEscapedKey()
        {
            if (string.IsNullOrWhiteSpace(_subscriptionKey))
            {
                throw new InvalidOperationException("Subscription key Azure Maps belum diisi.");
            }

            return Uri.EscapeDataString(_subscriptionKey);
        }

        private static string Fetch(string url)
        {
            return Client.GetStringAsync(url).GetAwaiter().GetResult();
        }

        private static bool TryNavigate(JsonElement element, string[] path, out JsonElement result)
        {
            result = element;
            foreach (string key in path)
            {
                if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty(key, out result))
                {
                    return false;
                }
            }
            return true;
        }

        private static double ReadDouble(JsonElement element, params string[] path)
        {
            return TryNavigate(element, path, out JsonElement value) && value.ValueKind == JsonValueKind.Number
                ? value.GetDouble()
                : 0;
        }

        private static string ReadString(JsonElement element, params string[] path)
        {
            return TryNavigate(element, path, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static DateTime ReadDate(JsonElement element, params string[] path)
        {
            return TryNavigate(element, path, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.String &&
                   value.TryGetDateTimeOffset(out DateTimeOffset date)
                ? date.LocalDateTime
                : DateTime.Now;
        }
    }
}
