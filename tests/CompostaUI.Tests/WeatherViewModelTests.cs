using Composta.Interfaces;
using Composta.Models;
using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class WeatherViewModelTests
    {
        [Fact]
        public async Task Refresh_UsesTrimmedLocationAndProviderTimestamp()
        {
            DateTime timestamp = new DateTime(2026, 11, 17, 9, 30, 15);
            StubWeatherService service = new StubWeatherService(_ => new WeatherData(31, 65, 2, "Rain", timestamp));
            WeatherViewModel vm = new WeatherViewModel(service) { Location = "  Bandung  " };

            await vm.RefreshCommand.ExecuteAsync(null);

            Assert.Equal("Bandung", service.LastLocation);
            Assert.Equal("Cuaca hari ini • Bandung", vm.LocationText);
            Assert.Equal("31°C", vm.TemperatureText);
            Assert.Contains("65%", vm.DetailText);
            Assert.EndsWith(timestamp.ToString("dd MMM yyyy HH:mm:ss"), vm.UpdatedAtText);
            Assert.False(vm.IsOffline);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public async Task OfflineProvider_IsExplicitlyLabeledAsSimulation()
        {
            WeatherViewModel vm = new WeatherViewModel(new OfflineWeatherService());
            await vm.RefreshCommand.ExecuteAsync(null);

            Assert.True(vm.IsOffline);
            Assert.Contains("simulasi", vm.StatusText);
            Assert.Contains("bukan cuaca aktual", vm.StatusText);
            Assert.StartsWith("Simulasi dibuat:", vm.UpdatedAtText);
            Assert.NotNull(vm.CurrentWeather);
        }

        [Fact]
        public async Task ProviderFailure_FallsBackAndCanBeRetriedWithoutLeakingError()
        {
            bool failing = true;
            StubWeatherService service = new StubWeatherService(_ => failing
                ? throw new InvalidOperationException("sensitive-api-key")
                : new WeatherData(30, 60, 0, "Clear", DateTime.Now));
            WeatherViewModel vm = new WeatherViewModel(service);

            await vm.RefreshCommand.ExecuteAsync(null);
            Assert.True(vm.IsOffline);
            Assert.Contains("Gagal", vm.StatusText);
            Assert.DoesNotContain("sensitive-api-key", vm.StatusText);
            Assert.False(vm.IsLoading);
            Assert.True(vm.RefreshCommand.CanExecute(null));

            failing = false;
            await vm.RefreshCommand.ExecuteAsync(null);
            Assert.False(vm.IsOffline);
            Assert.Equal("Clear", vm.ConditionText);
        }

        [Fact]
        public async Task BlankLocation_DoesNotFetchOrRelabelPreviousData()
        {
            StubWeatherService service = new StubWeatherService(_ => new WeatherData(30, 60, 0, "Clear", DateTime.Now));
            WeatherViewModel vm = new WeatherViewModel(service) { Location = "Yogyakarta" };
            await vm.RefreshCommand.ExecuteAsync(null);
            WeatherData? previous = vm.CurrentWeather;

            vm.Location = "  ";
            await vm.RefreshCommand.ExecuteAsync(null);
            Assert.Equal(1, service.Calls);
            Assert.Same(previous, vm.CurrentWeather);
            Assert.EndsWith("Yogyakarta", vm.LocationText);
            Assert.Contains("Isi lokasi", vm.StatusText);
        }

        [Fact]
        public async Task SlowProvider_DisablesRefreshUntilComplete()
        {
            using ManualResetEventSlim release = new ManualResetEventSlim(false);
            StubWeatherService service = new StubWeatherService(_ =>
            {
                release.Wait(TimeSpan.FromSeconds(5));
                return new WeatherData(30, 60, 0, "Clear", DateTime.Now);
            });
            WeatherViewModel vm = new WeatherViewModel(service);
            Task refresh = vm.RefreshCommand.ExecuteAsync(null);
            try
            {
                Assert.True(vm.IsLoading);
                Assert.Contains("Memuat", vm.StatusText);
                Assert.False(vm.RefreshCommand.CanExecute(null));
            }
            finally
            {
                release.Set();
                await refresh;
            }
            Assert.False(vm.IsLoading);
            Assert.True(vm.RefreshCommand.CanExecute(null));
        }

        [Fact]
        public async Task Timeout_FallsBackAndLateResultCannotOverwriteCard()
        {
            using ManualResetEventSlim release = new ManualResetEventSlim(false);
            TaskCompletionSource finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            StubWeatherService service = new StubWeatherService(_ =>
            {
                release.Wait(TimeSpan.FromSeconds(5));
                finished.SetResult();
                return new WeatherData(99, 0, 0, "Late result", DateTime.Now);
            });
            WeatherViewModel vm = new WeatherViewModel(service, TimeSpan.FromMilliseconds(50));
            try
            {
                await vm.RefreshCommand.ExecuteAsync(null);
                Assert.True(vm.IsOffline);
                Assert.False(vm.IsLoading);
            }
            finally
            {
                release.Set();
                await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            Assert.NotEqual("Late result", vm.ConditionText);
        }

        [Theory]
        [InlineData("Clear", 0, "☀")]
        [InlineData("Cerah", 0, "☀")]
        [InlineData("Clouds", 0, "☁")]
        [InlineData("Cerah Berawan", 0, "☁")]
        [InlineData("Rain", 0, "🌧")]
        [InlineData("Hujan Ringan", 0, "🌧")]
        [InlineData("Clear", 3, "🌧")]
        [InlineData("Thunderstorm", 2, "⛈")]
        [InlineData("Unknown", 0, "?")]
        public async Task Icon_FollowsWeatherCondition(string condition, double rainfall, string expected)
        {
            WeatherViewModel vm = new WeatherViewModel(new StubWeatherService(_ =>
                new WeatherData(30, 60, rainfall, condition, DateTime.Now)));
            await vm.RefreshCommand.ExecuteAsync(null);
            Assert.Equal(expected, vm.Icon);
        }

        [Fact]
        public async Task Refresh_UpdatesSelectedBatchRecommendationUsingDisplayedWeather()
        {
            bool raining = false;
            StubWeatherService service = new StubWeatherService(_ =>
                new WeatherData(29, 70, raining ? 5 : 0, raining ? "Rain" : "Clear", DateTime.Now));
            WeatherViewModel weather = new WeatherViewModel(service);
            MainWindowViewModel dashboard = new MainWindowViewModel(new FakeDialogService(), weather);
            CompostBatch batch = new CompostBatch();
            batch.AddWasteItem(new CompostableWaste("Daun", 2, null, Composta.Enums.BrownGreenType.Brown, 50));
            batch.AddWasteItem(new CompostableWaste("Sayur", 1, null, Composta.Enums.BrownGreenType.Green, 50));
            dashboard.SelectedBatch = new BatchItemViewModel(batch);

            await weather.RefreshCommand.ExecuteAsync(null);
            Assert.Contains("Kondisi kompos sudah baik", dashboard.RecommendationText);

            raining = true;
            await weather.RefreshCommand.ExecuteAsync(null);
            Assert.Contains("Sedang hujan", dashboard.RecommendationText);
            Assert.Equal("Rain", weather.CurrentWeather?.Condition);
            Assert.Equal(2, service.Calls);
        }

        private sealed class StubWeatherService : IWeatherService
        {
            private readonly Func<string, WeatherData> _getWeather;
            public StubWeatherService(Func<string, WeatherData> getWeather) => _getWeather = getWeather;
            public string? LastLocation { get; private set; }
            public int Calls { get; private set; }
            public WeatherData GetCurrentWeather(string loc)
            {
                Calls++;
                LastLocation = loc;
                return _getWeather(loc);
            }
            public List<WeatherData> GetForecast(string loc, int days) => new List<WeatherData>();
        }
    }
}
