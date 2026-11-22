using Composta.Interfaces;
using Composta.Models;

namespace Composta.Services
{
    public class CompostAdvisorService : IRecommendationEngine
    {
        public const int DefaultMaturityDays = 60;
        public const int MinimumMaturityDays = 14;
        public const int DefaultForecastDays = 5;

        public const double DefaultMinimumRatio = 2.0;
        public const double DefaultMaximumRatio = 3.0;

        private const double WetMinimumRatio = 2.5;
        private const double WetMaximumRatio = 3.5;

        private const double HotDryMinimumRatio = 1.5;
        private const double HotDryMaximumRatio = 2.5;

        private IWeatherService _weatherService;

        public CompostAdvisorService(IWeatherService weatherService)
        {
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        }

        public IWeatherService WeatherService
        {
            get { return _weatherService; }
            set { _weatherService = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        public static int EstimateMaturityDays(IReadOnlyList<WeatherData>? forecast)
        {
            List<WeatherData>? validForecast = forecast?
                .Where(w => w != null)
                .ToList();

            if (validForecast == null || validForecast.Count == 0)
            {
                return DefaultMaturityDays;
            }

            double averageTemperature = validForecast.Average(w => w.Temperature);
            double averageHumidity = validForecast.Average(w => w.Humidity);

            int temperatureAdjustment;
            if (averageTemperature >= 25 && averageTemperature <= 35)
            {
                temperatureAdjustment = -8;
            }
            else if (averageTemperature > 35)
            {
                temperatureAdjustment = 5;
            }
            else if (averageTemperature < 15)
            {
                temperatureAdjustment = 12;
            }
            else
            {
                temperatureAdjustment = 4;
            }

            int humidityAdjustment = (averageHumidity < 45 || averageHumidity > 85) ? 6 : 0;

            int estimatedDays = DefaultMaturityDays + temperatureAdjustment + humidityAdjustment;
            return Math.Max(MinimumMaturityDays, estimatedDays);
        }

        private static readonly TimeSpan ForecastTimeout = TimeSpan.FromSeconds(5);

        public int EstimateMaturityDays(string loc, int days = DefaultForecastDays)
        {
            List<WeatherData>? forecast;
            try
            {
                forecast = Task.Run(() => _weatherService.GetForecast(loc, days))
                    .WaitAsync(ForecastTimeout)
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                forecast = null;
            }

            return EstimateMaturityDays(forecast);
        }

        public static (double MinRatio, double MaxRatio) GetTargetRatio(WeatherData? weather)
        {
            if (weather == null)
            {
                return (DefaultMinimumRatio, DefaultMaximumRatio);
            }

            if (weather.IsRainy() || weather.Humidity > 85)
            {
                return (WetMinimumRatio, WetMaximumRatio);
            }

            if (weather.Temperature > 32 && weather.Humidity < 50)
            {
                return (HotDryMinimumRatio, HotDryMaximumRatio);
            }

            return (DefaultMinimumRatio, DefaultMaximumRatio);
        }

        public CompostRecommendation GenerateRecommendation(CompostBatch batch, string loc)
        {
            ArgumentNullException.ThrowIfNull(batch);

            WeatherData? weather;
            try
            {
                weather = _weatherService.GetCurrentWeather(loc);
            }
            catch (Exception ex) when (ex is HttpRequestException or TimeoutException or InvalidOperationException)
            {
                weather = null;
            }

            return GenerateRecommendationInternal(batch, weather);
        }

        public CompostRecommendation GenerateRecommendation(CompostBatch batch, WeatherData weather)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(weather);

            return GenerateRecommendationInternal(batch, weather);
        }

        private static CompostRecommendation GenerateRecommendationInternal(CompostBatch batch, WeatherData? weather)
        {
            batch.UpdateProgress();

            if (batch.IsReadyToHarvest())
            {
                return new CompostRecommendation(batch.Id, "Kompos sudah matang dan siap dipanen.", "High");
            }

            List<string> advice = new List<string>();
            int score = 0;

            (double minRatio, double maxRatio) = GetTargetRatio(weather);
            string targetText = $"{minRatio:0.#}-{maxRatio:0.#}";

            double ratio = batch.CalculateRatio();
            if (batch.WasteItems.Count == 0)
            {
                advice.Add($"Batch masih kosong, tambahkan bahan brown dan green (target rasio {targetText}).");
                score += 1;
            }
            else if (!batch.HasGreenMaterial())
            {
                advice.Add($"Belum ada bahan green (target rasio {targetText}), tambahkan sisa sayur atau buah agar kompos bisa terurai.");
                score += 2;
            }
            else if (ratio < minRatio)
            {
                advice.Add($"Rasio brown/green {ratio:0.##} terlalu rendah (target {targetText}), tambahkan bahan brown seperti daun kering atau kardus.");
                score += 2;
            }
            else if (ratio > maxRatio)
            {
                advice.Add($"Rasio brown/green {ratio:0.##} terlalu tinggi (target {targetText}), tambahkan bahan green seperti sisa sayur atau buah.");
                score += 1;
            }

            if (weather != null)
            {
                if (weather.IsRainy())
                {
                    advice.Add("Sedang hujan, tutup tumpukan kompos agar tidak terlalu basah.");
                    score += 2;
                }
                else if (weather.Temperature > 32 && weather.Humidity < 50)
                {
                    advice.Add("Cuaca panas dan kering, siram kompos agar tetap lembap.");
                    score += 2;
                }
                else if (weather.Humidity > 85)
                {
                    advice.Add("Kelembapan udara tinggi, aduk kompos untuk menambah sirkulasi udara.");
                    score += 1;
                }

                if (weather.Temperature < 15)
                {
                    advice.Add("Suhu rendah memperlambat pengomposan, tutup kompos agar panas tertahan.");
                    score += 1;
                }
            }

            bool rainy = weather?.IsRainy() ?? false;
            if (!rainy && batch.WasteItems.OfType<CompostableWaste>().Any(w => w.NeedsWater()))
            {
                advice.Add("Beberapa bahan terlalu kering, tambahkan air secukupnya.");
                score += 1;
            }

            int overdueCount = batch.Schedules.Count(s => s.IsOverdue());
            if (overdueCount > 0)
            {
                advice.Add($"Ada {overdueCount} jadwal perawatan yang terlewat.");
                score += 2;
            }

            if (advice.Count == 0)
            {
                advice.Add($"Kondisi kompos sudah baik (target rasio {targetText}), lanjutkan perawatan rutin.");
            }

            string priority = score >= 4 ? "High" : score >= 2 ? "Medium" : "Low";
            return new CompostRecommendation(batch.Id, string.Join(" ", advice), priority);
        }
    }
}
