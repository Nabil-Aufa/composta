using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Composta.Enums;
using Composta.Models;
using Composta.Services;

namespace Composta.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;
        private readonly IUserSessionService _session;

        [ObservableProperty]
        private string _greeting = string.Empty;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddWasteCommand))]
        [NotifyCanExecuteChangedFor(nameof(NewBatchCommand))]
        [NotifyCanExecuteChangedFor(nameof(HarvestCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteBatchCommand))]
        [NotifyCanExecuteChangedFor(nameof(ShowProfileCommand))]
        [NotifyCanExecuteChangedFor(nameof(CompleteScheduleCommand))]
        private bool _isDatabaseConnected;

        [ObservableProperty]
        private string _databaseStatusText = "Menghubungkan ke PostgreSQL...";

        [ObservableProperty]
        private string _totalCo2eText = "-";

        [ObservableProperty]
        private string _activeBatchCountText = "-";

        [ObservableProperty]
        private string _totalWeightText = "-";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(HarvestCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteBatchCommand))]
        private BatchItemViewModel? _selectedBatch;

        [ObservableProperty]
        private bool _hasNoBatches;

        [ObservableProperty]
        private string _recommendationTitle = "Rekomendasi";

        [ObservableProperty]
        private string _recommendationText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PriorityText))]
        [NotifyPropertyChangedFor(nameof(IsHighPriority))]
        [NotifyPropertyChangedFor(nameof(IsMediumPriority))]
        [NotifyPropertyChangedFor(nameof(IsLowPriority))]
        private string _priority = "Low";

        public MainWindowViewModel(
            IDialogService dialogs,
            WeatherViewModel? weather = null,
            IUserSessionService? session = null)
        {
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _session = session ?? new UserSessionService();

            Weather = weather ?? new WeatherViewModel(AppData.WeatherService);
            Weather.WeatherUpdated += () =>
            {
                if (SelectedBatch != null)
                {
                    ShowRecommendation(SelectedBatch.Batch);
                }
            };

            Batches = new ObservableCollection<BatchItemViewModel>();
            ShowEmptyRecommendation();
        }

        public ObservableCollection<BatchItemViewModel> Batches { get; }
        public ObservableCollection<MaintenanceScheduleItemViewModel> MaintenanceSchedules { get; } = new();
        [ObservableProperty]
        private bool _hasNoMaintenanceSchedules = true;

        [ObservableProperty]
        private string _maintenanceEmptyText = "Jadwal akan muncul setelah ada batch kompos yang dipilih.";

        public WeatherViewModel Weather { get; }
        public Co2eChartViewModel Co2eChart { get; } = new();

        public event Action? LogoutRequested;

        [RelayCommand(CanExecute = nameof(CanModifyData))]
        private Task ShowProfileAsync()
        {
            return _dialogs.ShowProfileAsync(new ProfileViewModel(AppData.CurrentUser, AppData.CarbonCalculator));
        }

        [RelayCommand]
        private void Logout()
        {
            _session.Logout();
            IsDatabaseConnected = false;
            Greeting = string.Empty;
            UserName = string.Empty;
            TotalCo2eText = "-";
            TotalWeightText = "-";
            ActiveBatchCountText = "-";
            SelectedBatch = null;
            Batches.Clear();
            Co2eChart.UpdateChart(Array.Empty<double>());
            HasNoBatches = true;
            ShowEmptyRecommendation();
            LogoutRequested?.Invoke();
        }

        public string PriorityText
        {
            get
            {
                switch (Priority)
                {
                    case "High":
                        return "Prioritas Tinggi";
                    case "Medium":
                        return "Prioritas Sedang";
                    default:
                        return "Prioritas Rendah";
                }
            }
        }

        public bool IsHighPriority
        {
            get { return Priority == "High"; }
        }

        public bool IsMediumPriority
        {
            get { return Priority == "Medium"; }
        }

        public bool IsLowPriority
        {
            get { return !IsHighPriority && !IsMediumPriority; }
        }

        public async Task LoadAsync()
        {
            await Task.WhenAll(
                LoadDashboardAsync(null),
                Weather.RefreshCommand.ExecuteAsync(null)
            );

            if (IsDatabaseConnected)
            {
                await ShowMaintenanceReminderAsync();
            }
        }

        private async Task ShowMaintenanceReminderAsync()
        {
            var schedules = AppData.CurrentUser.Batches
                .SelectMany(batch => batch.Schedules)
                .Where(schedule =>
                    !schedule.IsCompleted &&
                    schedule.DueDate.Date <= DateTime.Today)
                .ToList();

            if (schedules.Count == 0)
            {
                return;
            }

            int overdueCount = schedules.Count(
                schedule => schedule.DueDate.Date < DateTime.Today);

            int todayCount = schedules.Count(
                schedule => schedule.DueDate.Date == DateTime.Today);

            string message =
                $"Ada {schedules.Count} jadwal perawatan yang perlu diperhatikan.\n\n" +
                $"Terlambat: {overdueCount}\n" +
                $"Jatuh tempo hari ini: {todayCount}\n\n" +
                "Pilih batch untuk melihat detail jadwal perawatan.";

            await _dialogs.ShowMessageAsync("Pengingat Perawatan", message);
        }


        private async Task LoadDashboardAsync(string? selectedBatchId)
        {
            try
            {
                AppData.Load();
            }
            catch (Exception ex)
            {
                IsDatabaseConnected = false;
                DatabaseStatusText = "Tidak terhubung ke PostgreSQL";
                await _dialogs.ShowMessageAsync("Database Gagal", DatabaseErrorMessage.From(ex, AppData.DatabaseInfo));
                return;
            }

            IsDatabaseConnected = true;
            DatabaseStatusText = "Terhubung ke PostgreSQL  •  " + AppData.DatabaseInfo;

            User user = AppData.CurrentUser;
            UserName = user.Name;
            Greeting = "Halo, " + user.Name + "! Ayo ubah sampah organik jadi kompos.";

            double totalCo2e = user.Batches.Sum(b => AppData.CarbonCalculator.CalculateBatchImpact(b));
            Co2eChart.UpdateFromBatches(
                user.Batches,
                AppData.CarbonCalculator
            );
            TotalCo2eText = totalCo2e.ToString("0.##") + " kg";
            ActiveBatchCountText = user.GetActiveBatches().Count.ToString();
            TotalWeightText = user.Batches.Sum(b => b.GetTotalWeight()).ToString("0.##") + " kg";

            Batches.Clear();
            foreach (CompostBatch batch in user.Batches)
            {
                Batches.Add(new BatchItemViewModel(batch));
            }

            HasNoBatches = Batches.Count == 0;

            SelectedBatch = Batches.FirstOrDefault(b => b.Batch.Id == selectedBatchId)
                ?? Batches.FirstOrDefault(b => !b.IsArchived)
                ?? Batches.FirstOrDefault();
            RefreshMaintenanceSchedules();
            if (SelectedBatch == null)
            {
                ShowEmptyRecommendation();
            }
        }

        partial void OnSelectedBatchChanged(BatchItemViewModel? value)
        {
            RefreshMaintenanceSchedules();
            if (value == null)
            {
                ShowEmptyRecommendation();
                return;
            }

            ShowRecommendation(value.Batch);
        }

        private void RefreshMaintenanceSchedules()
        {
            MaintenanceSchedules.Clear();

            if (SelectedBatch != null)
            {
                foreach (MaintenanceSchedule schedule in SelectedBatch.Batch.Schedules.OrderBy(s => s.DueDate))
                {
                    MaintenanceSchedules.Add(new MaintenanceScheduleItemViewModel(schedule));
                }
            }

            HasNoMaintenanceSchedules = MaintenanceSchedules.Count == 0;
            MaintenanceEmptyText = SelectedBatch == null
                ? "Jadwal akan muncul setelah ada batch kompos yang dipilih."
                : "Batch ini belum punya jadwal perawatan.";
        }

        private void ShowRecommendation(CompostBatch batch)
        {
            RecommendationTitle = "Rekomendasi " + AppData.GetBatchName(batch);

            if (batch.Status == BatchStatus.Archived)
            {
                RecommendationText = "Kompos dari batch ini sudah dipanen.";
                Priority = "Low";
                return;
            }

            WeatherData? weather = Weather.CurrentWeather;
            if (weather == null)
            {
                RecommendationText = "Muat cuaca untuk melihat rekomendasi perawatan.";
                Priority = "Low";
                return;
            }
            CompostRecommendation recommendation = AppData.Advisor.GenerateRecommendation(batch, weather);
            RecommendationText = recommendation.Message;
            Priority = recommendation.Priority;
        }

        private void ShowEmptyRecommendation()
        {
            RecommendationTitle = "Rekomendasi";
            RecommendationText = "Belum ada batch yang dipilih.";
            Priority = "Low";
        }

        private bool CanModifyData()
        {
            return IsDatabaseConnected;
        }

        private bool CanHarvest()
        {
            return IsDatabaseConnected && SelectedBatch != null && SelectedBatch.IsReady;
        }

        private bool CanDeleteBatch()
        {
            return IsDatabaseConnected && SelectedBatch != null;
        }

        private bool CanCompleteSchedule(MaintenanceScheduleItemViewModel? item)
        {
            return IsDatabaseConnected && item != null && item.CanComplete;
        }

        [RelayCommand(CanExecute = nameof(CanCompleteSchedule))]
        private async Task CompleteScheduleAsync(MaintenanceScheduleItemViewModel? item)
        {
            if (item == null || item.IsCompleted)
            {
                return;
            }

            try
            {
                AppData.CompleteSchedule(item.Schedule);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Update Gagal",
                    item.TaskName + " gagal ditandai selesai.\n\n" + ex.Message);
                return;
            }

            RefreshMaintenanceSchedules();
            if (SelectedBatch != null)
            {
                ShowRecommendation(SelectedBatch.Batch);
            }
        }

        [RelayCommand(CanExecute = nameof(CanModifyData))]
        private async Task AddWasteAsync()
        {
            AddWasteViewModel addWaste = new AddWasteViewModel(SelectedBatch?.Batch);
            bool saved = await _dialogs.ShowAddWasteAsync(addWaste);
            if (saved)
            {
                await LoadDashboardAsync(addWaste.SavedBatchId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanModifyData))]
        private async Task NewBatchAsync()
        {
            CompostBatch batch = new CompostBatch(AppData.CurrentUser.Id, DateTime.Now, 60);
            try
            {
                AppData.BatchRepository.Add(batch);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Insert Gagal", "Batch baru gagal disimpan.\n\n" + ex.Message);
                return;
            }

            await LoadDashboardAsync(batch.Id);

            CompostBatch? savedBatch = AppData.CurrentUser.Batches.FirstOrDefault(b => b.Id == batch.Id);
            string batchName = savedBatch != null ? AppData.GetBatchName(savedBatch) : "Batch baru";
            await _dialogs.ShowMessageAsync("Berhasil", batchName + " berhasil ditambahkan ke database.");
        }

        [RelayCommand(CanExecute = nameof(CanHarvest))]
        private async Task HarvestAsync()
        {
            CompostBatch? batch = SelectedBatch?.Batch;
            if (batch == null || batch.Status != BatchStatus.Ready)
            {
                await _dialogs.ShowMessageAsync("Panen", "Pilih batch yang berstatus Siap Panen terlebih dahulu.");
                return;
            }

            string batchName = AppData.GetBatchName(batch);
            batch.Status = BatchStatus.Archived;
            try
            {
                AppData.BatchRepository.Update(batch);
            }
            catch (Exception ex)
            {
                batch.Status = BatchStatus.Ready;
                await _dialogs.ShowMessageAsync("Update Gagal", batchName + " gagal diperbarui.\n\n" + ex.Message);
                return;
            }

            await LoadDashboardAsync(batch.Id);
            await _dialogs.ShowMessageAsync("Berhasil", batchName + " berhasil dipanen.");
        }

        [RelayCommand(CanExecute = nameof(CanDeleteBatch))]
        private async Task DeleteBatchAsync()
        {
            CompostBatch? batch = SelectedBatch?.Batch;
            if (batch == null)
            {
                await _dialogs.ShowMessageAsync("Hapus Batch", "Mohon pilih batch yang akan dihapus.");
                return;
            }

            string batchName = AppData.GetBatchName(batch);
            bool confirmed = await _dialogs.ConfirmAsync("Konfirmasi",
                "Yakin ingin menghapus " + batchName + " beserta seluruh sampah di dalamnya?");
            if (!confirmed)
            {
                return;
            }

            try
            {
                AppData.BatchRepository.Delete(batch.Id);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Delete Gagal", batchName + " gagal dihapus.\n\n" + ex.Message);
                return;
            }

            await LoadDashboardAsync(null);
            await _dialogs.ShowMessageAsync("Berhasil", batchName + " berhasil dihapus dari database.");
        }
    }
}
