using System.Net;
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
        private string _icon = string.Empty;

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
                StatusText = "Isi lokasi terlebih dahulu, lalu tekan Muat ulang.";
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
                    catch (Exception ex)
                    {
                        weather = _offline.GetCurrentWeather(location);
                        IsOffline = true;
                        StatusText = GetFailureText(ex, location);
                    }
                }

                CurrentWeather = weather;
                LocationText = "Cuaca · " + location;
                TemperatureText = weather.Temperature.ToString("0.#") + "°C";
                ConditionText = TranslateCondition(weather.Condition);
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

        private static readonly Dictionary<string, string> ConditionNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Thunderstorm"] = "Badai petir",
            ["Drizzle"] = "Gerimis",
            ["Rain"] = "Hujan",
            ["Snow"] = "Salju",
            ["Clear"] = "Cerah",
            ["Clouds"] = "Berawan",
            ["Mist"] = "Berkabut",
            ["Fog"] = "Kabut tebal",
            ["Haze"] = "Berkabut tipis",
            ["Smoke"] = "Berasap",
            ["Dust"] = "Berdebu",
            ["Sand"] = "Berpasir",
            ["Ash"] = "Abu vulkanik",
            ["Squall"] = "Angin kencang",
            ["Tornado"] = "Tornado"
        };

        public static string TranslateCondition(string condition)
        {
            string trimmed = condition?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return "Kondisi tidak diketahui";
            }

            return ConditionNames.TryGetValue(trimmed, out string? name) ? name : trimmed;
        }

        private static string GetFailureText(Exception ex, string location)
        {
            const string Fallback = " Memakai data simulasi offline.";

            HttpRequestException? http = FindException<HttpRequestException>(ex);
            if (http?.StatusCode == HttpStatusCode.NotFound)
            {
                return "Gagal memuat cuaca: lokasi \"" + location + "\" tidak ditemukan. Periksa ejaan lokasi." + Fallback;
            }

            if (http?.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Gagal memuat cuaca: API key cuaca tidak valid." + Fallback;
            }

            if (http != null || FindException<TimeoutException>(ex) != null || FindException<TaskCanceledException>(ex) != null)
            {
                return "Gagal memuat cuaca: tidak dapat terhubung ke layanan cuaca." + Fallback + " Coba tekan Muat ulang lagi.";
            }

            return "Gagal memuat cuaca." + Fallback + " Coba tekan Muat ulang lagi.";
        }

        private static T? FindException<T>(Exception ex) where T : Exception
        {
            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                if (current is T match)
                {
                    return match;
                }
            }

            return null;
        }

        private static string GetIcon(WeatherData weather)
        {
            string condition = weather.Condition.ToLowerInvariant();
            if (condition.Contains("thunder") || condition.Contains("petir")) return "⛈";
            if (weather.IsRainy() || condition.Contains("drizzle") || condition.Contains("gerimis")) return "🌧";
            if (condition.Contains("snow") || condition.Contains("salju")) return "❄";
            if (condition.Contains("mist") || condition.Contains("fog") || condition.Contains("haze") || condition.Contains("kabut")) return "🌫";
            if (condition.Contains("cloud") || condition.Contains("berawan")) return "☁";
            if (condition.Contains("clear") || condition.Contains("sun") || condition.Contains("cerah")) return "☀";
            return "🌡";
        }
    }
}
