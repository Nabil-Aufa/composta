using Composta.Interfaces;
using Composta.Models;

namespace Composta.Services
{
    public class CompostAdvisorService : IRecommendationEngine
    {
        private const double MinimumRatio = 2.0;
        private const double MaximumRatio = 3.0;

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

        public CompostRecommendation GenerateRecommendation(CompostBatch batch, string loc)
        {
            WeatherData weather = _weatherService.GetCurrentWeather(loc);
            return GenerateRecommendation(batch, weather);
        }

        public CompostRecommendation GenerateRecommendation(CompostBatch batch, WeatherData weather)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(weather);

            batch.UpdateProgress();

            if (batch.IsReadyToHarvest())
            {
                return new CompostRecommendation(batch.Id, "Kompos sudah matang dan siap dipanen.", "High");
            }

            List<string> advice = new List<string>();
            int score = 0;

            double ratio = batch.CalculateRatio();
            if (batch.WasteItems.Count == 0)
            {
                advice.Add("Batch masih kosong, tambahkan bahan brown dan green.");
                score += 1;
            }
            else if (ratio < MinimumRatio)
            {
                advice.Add($"Rasio brown/green {ratio:0.##} terlalu rendah, tambahkan bahan brown seperti daun kering atau kardus.");
                score += 2;
            }
            else if (ratio > MaximumRatio)
            {
                advice.Add($"Rasio brown/green {ratio:0.##} terlalu tinggi, tambahkan bahan green seperti sisa sayur atau buah.");
                score += 1;
            }

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

            if (!weather.IsRainy() && batch.WasteItems.OfType<CompostableWaste>().Any(w => w.NeedsWater()))
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
                advice.Add("Kondisi kompos sudah baik, lanjutkan perawatan rutin.");
            }

            string priority = score >= 4 ? "High" : score >= 2 ? "Medium" : "Low";
            return new CompostRecommendation(batch.Id, string.Join(" ", advice), priority);
        }
    }
}
