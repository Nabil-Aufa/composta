using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Composta.Interfaces;
using Composta.Models;
using Composta.Services;

namespace Composta.ViewModels
{
    public partial class WeatherViewModel : ObservableObject
    {
        private readonly IWeatherService _service;
        private readonly OfflineWeatherService _offline = new OfflineWeatherService();
        private readonly TimeSpan _timeout;

        [ObservableProperty]
        private string _location = AppData.Location;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isOffline;

        [ObservableProperty]
        private string _locationText = "Cuaca hari ini";

        [ObservableProperty]
        private string _temperatureText = "—";

        [ObservableProperty]
        private string _conditionText = string.Empty;

        [ObservableProperty]
        private string _detailText = string.Empty;

        [ObservableProperty]
        private string _icon = "?";

        [ObservableProperty]
        private string _statusText = "Cuaca belum dimuat.";

        [ObservableProperty]
        private string _updatedAtText = "Belum ada pembaruan.";

        public WeatherViewModel(IWeatherService service, TimeSpan? timeout = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _timeout = timeout ?? TimeSpan.FromSeconds(10);
            if (_timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }

        public WeatherData? CurrentWeather { get; private set; }

        public event Action? WeatherUpdated;

        private bool CanRefresh() => !IsLoading;

        [RelayCommand(CanExecute = nameof(CanRefresh))]
        private async Task RefreshAsync()
        {
            if (IsLoading)
            {
                return;
            }

            string location = Location?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(location))
            {
                StatusText = "Isi lokasi terlebih dahulu, lalu tekan Refresh.";
                return;
            }

            IsLoading = true;
            StatusText = "Memuat cuaca...";
            try
            {
                WeatherData weather;
                if (_service is OfflineWeatherService)
                {
                    weather = _offline.GetCurrentWeather(location);
                    IsOffline = true;
                    StatusText = "Mode offline • data simulasi, bukan cuaca aktual lokasi.";
                }
                else
                {
                    try
                    {
                        weather = await Task.Run(() => _service.GetCurrentWeather(location)).WaitAsync(_timeout);
                        IsOffline = false;
                        StatusText = "Data cuaca berhasil diperbarui.";
                    }
                    catch (Exception)
                    {
                        weather = _offline.GetCurrentWeather(location);
                        IsOffline = true;
                        StatusText = "Gagal memuat cuaca • memakai data simulasi offline. Coba Refresh lagi.";
                    }
                }

                CurrentWeather = weather;
                LocationText = "Cuaca · " + location;
                TemperatureText = weather.Temperature.ToString("0.#") + "°C";
                ConditionText = weather.Condition;
                DetailText = "Kelembapan " + weather.Humidity.ToString("0") + "% • Curah hujan "
                    + weather.RainfallMm.ToString("0.#") + " mm";
                Icon = GetIcon(weather);
                UpdatedAtText = (IsOffline ? "Simulasi dibuat: " : "Data diperbarui: ")
                    + weather.RetrievedAt.ToString("dd MMM yyyy HH:mm:ss");
                WeatherUpdated?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static string GetIcon(WeatherData weather)
        {
            string condition = weather.Condition.ToLowerInvariant();
            if (condition.Contains("thunder") || condition.Contains("petir")) return "⛈";
            if (weather.IsRainy() || condition.Contains("drizzle") || condition.Contains("gerimis")) return "🌧";
            if (condition.Contains("snow") || condition.Contains("salju")) return "❄";
            if (condition.Contains("mist") || condition.Contains("fog") || condition.Contains("kabut")) return "🌫";
            if (condition.Contains("cloud") || condition.Contains("berawan")) return "☁";
            if (condition.Contains("clear") || condition.Contains("sun") || condition.Contains("cerah")) return "☀";
            return "?";
        }
    }
}
