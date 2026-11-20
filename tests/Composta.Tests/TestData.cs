using Composta.Enums;
using Composta.Interfaces;
using Composta.Models;

namespace Composta.Tests
{
    internal static class TestData
    {
        public static readonly WasteCategory Vegetable = new WasteCategory("Sisa Sayur", 0.5, true);
        public static readonly WasteCategory DryLeaves = new WasteCategory("Daun Kering", 0.3, true);
        public static readonly WasteCategory Plastic = new WasteCategory("Plastik", 0, false);

        public static WeatherData NeutralWeather()
        {
            return new WeatherData(27, 60, 0, "Cerah", DateTime.Now);
        }

        public static CompostBatch NewBatch(int startedDaysAgo = 0, int maturityDays = 60)
        {
            return new CompostBatch("user-1", DateTime.Now.AddDays(-startedDaysAgo), maturityDays);
        }

        public static CompostBatch BatchWithRatio(double brownKg, double greenKg, double moisture = 60)
        {
            CompostBatch batch = NewBatch();
            if (brownKg > 0)
            {
                batch.AddWasteItem(Brown(brownKg, moisture));
            }

            if (greenKg > 0)
            {
                batch.AddWasteItem(Green(greenKg, moisture));
            }

            return batch;
        }

        public static CompostableWaste Brown(double weight, double moisture = 60)
        {
            return new CompostableWaste("Daun kering", weight, DryLeaves, BrownGreenType.Brown, moisture);
        }

        public static CompostableWaste Green(double weight, double moisture = 60)
        {
            return new CompostableWaste("Sisa sayur", weight, Vegetable, BrownGreenType.Green, moisture);
        }
    }

    internal class FakeWeatherService : IWeatherService
    {
        private readonly WeatherData _weather;

        public FakeWeatherService(WeatherData weather)
        {
            _weather = weather;
        }

        public WeatherData GetCurrentWeather(string loc)
        {
            return _weather;
        }

        public List<WeatherData> GetForecast(string loc, int days)
        {
            return Enumerable.Repeat(_weather, days).ToList();
        }
    }
}
