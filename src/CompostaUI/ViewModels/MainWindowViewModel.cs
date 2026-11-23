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

        public Task MaturityRefreshTask { get; private set; } = Task.CompletedTask;

        public async Task LoadAsync()
        {
            await Task.WhenAll(
                LoadDashboardAsync(null),
                Weather.RefreshCommand.ExecuteAsync(null)
            );

            if (IsDatabaseConnected)
            {
                MaturityRefreshTask = RefreshMaturityEstimateInBackgroundAsync();
                await ShowMaintenanceReminderAsync();
            }
        }

        
        private async Task RefreshMaturityEstimateInBackgroundAsync()
        {
            if (AppData.HasRefreshedMaturityDays)
            {
                return;
            }

            string baseStatus = "Terhubung ke PostgreSQL  •  " + AppData.DatabaseInfo;
            DatabaseStatusText = baseStatus + "  •  Menghitung estimasi panen...";

            int estimatedDays;
            try
            {
                estimatedDays = await Task.Run(AppData.RefreshEstimatedMaturityDays);
            }
            finally
            {
                if (IsDatabaseConnected)
                {
                    DatabaseStatusText = baseStatus;
                }
            }

            if (!IsDatabaseConnected)
            {
                return;
            }

            List<CompostBatch> batchesSnapshot = AppData.CurrentUser.Batches.ToList();
            bool anyUpdated = false;
            string? syncError = null;

            foreach (CompostBatch batch in batchesSnapshot)
            {
                BatchStatus oldStatus = batch.Status;
                DateTime oldMaturityDate = batch.EstimatedMaturityDate;

                batch.RecalculateEstimatedMaturityDate(estimatedDays);
                batch.UpdateProgress();

                if (batch.Status != oldStatus || batch.EstimatedMaturityDate != oldMaturityDate)
                {
                    try
                    {
                        AppData.BatchRepository.Update(batch, AppData.CurrentUser.Id);
                        anyUpdated = true;
                    }
                    catch (Exception ex)
                    {
                        syncError ??= DatabaseErrorMessage.From(ex, AppData.DatabaseInfo);
                    }
                }
            }

            if (syncError != null)
            {
                await _dialogs.ShowMessageAsync(
                    "Sinkronisasi Estimasi Gagal",
                    "Estimasi waktu matang terbaru gagal disimpan ke database.\n\n" + syncError,
                    MessageTone.Warning);
            }

            if (anyUpdated && IsDatabaseConnected)
            {
                string? currentSelectedId = SelectedBatch?.Batch.Id;
                List<CompostBatch> latestBatches = AppData.CurrentUser.Batches.ToList();

                Batches.Clear();
                foreach (CompostBatch batch in latestBatches)
                {
                    Batches.Add(new BatchItemViewModel(batch));
                }

                SelectedBatch = Batches.FirstOrDefault(b => b.Batch.Id == currentSelectedId)
                    ?? Batches.FirstOrDefault(b => !b.IsArchived)
                    ?? Batches.FirstOrDefault();
                RefreshMaintenanceSchedules();
            }
        }

        private async Task ShowMaintenanceReminderAsync()
        {
            var schedules = AppData.CurrentUser.Batches
                .Where(batch => batch.Status != BatchStatus.Archived)
                .SelectMany(batch => batch.Schedules
                    .Where(schedule =>
                        !schedule.IsCompleted &&
                        schedule.DueDate.Date <= DateTime.Today)
                    .Select(schedule => (Batch: batch, Schedule: schedule)))
                .OrderBy(item => item.Schedule.DueDate)
                .ToList();

            if (schedules.Count == 0)
            {
                return;
            }

            int overdueCount = schedules.Count(
                item => item.Schedule.DueDate.Date < DateTime.Today);

            int todayCount = schedules.Count(
                item => item.Schedule.DueDate.Date == DateTime.Today);

            const int MaxListed = 5;
            IEnumerable<string> lines = schedules
                .Take(MaxListed)
                .Select(item =>
                {
                    int lateDays = (DateTime.Today - item.Schedule.DueDate.Date).Days;
                    string when = lateDays > 0 ? $"terlambat {lateDays} hari" : "hari ini";
                    string task = new MaintenanceScheduleItemViewModel(item.Schedule).TaskName;
                    return $"•  {AppData.GetBatchName(item.Batch)}: {task} ({when})";
                });

            string list = string.Join("\n", lines);
            if (schedules.Count > MaxListed)
            {
                list += $"\n•  dan {schedules.Count - MaxListed} jadwal lainnya";
            }

            string message =
                $"Ada {schedules.Count} jadwal perawatan yang perlu diperhatikan " +
                $"(Terlambat: {overdueCount}, jatuh tempo hari ini: {todayCount}).\n\n" +
                list + "\n\n" +
                "Pilih batch tersebut untuk menandai jadwal sebagai selesai.";

            await _dialogs.ShowMessageAsync("Pengingat Perawatan", message, MessageTone.Warning);
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
                await _dialogs.ShowMessageAsync("Database Gagal", DatabaseErrorMessage.From(ex, AppData.DatabaseInfo), MessageTone.Error);
                return;
            }

            IsDatabaseConnected = true;
            DatabaseStatusText = "Terhubung ke PostgreSQL  •  " + AppData.DatabaseInfo;

            User user = AppData.CurrentUser;
            UserName = user.Name;
            Greeting = "Halo, " + user.Name + "! Ayo ubah sampah organik jadi kompos.";

            double totalCo2e = AppData.CarbonCalculator.CalculateCO2eSaved(user.Batches.SelectMany(b => b.WasteItems).ToList());
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

            await LoadCo2eChartAsync();
        }

        private async Task LoadCo2eChartAsync()
        {
            try
            {
                Co2eChart.UpdateFromTimeline(AppData.GetCO2eTimeline());
            }
            catch (Exception ex)
            {
                Co2eChart.UpdateFromTimeline(Array.Empty<CO2eTimelinePoint>());
                await _dialogs.ShowMessageAsync("Grafik CO2e Gagal Dimuat",
                    DatabaseErrorMessage.From(ex, AppData.DatabaseInfo), MessageTone.Error);
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
            SaveRecommendationHistory(recommendation);
        }

        private void SaveRecommendationHistory(CompostRecommendation recommendation)
        {
            if (!IsDatabaseConnected || Weather.IsOffline)
            {
                return;
            }

            try
            {
                AppData.SaveRecommendationIfChanged(recommendation);
            }
            catch (Exception ex)
            {
                if (RecommendationHistoryFailed)
                {
                    return;
                }

                RecommendationHistoryFailed = true;
                _ = _dialogs.ShowMessageAsync("Riwayat Rekomendasi Gagal Disimpan",
                    "Rekomendasi tetap ditampilkan, tetapi riwayatnya tidak tersimpan.\n\n"
                    + DatabaseErrorMessage.From(ex, AppData.DatabaseInfo), MessageTone.Warning);
            }
        }

        public bool RecommendationHistoryFailed { get; private set; }

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
            return IsDatabaseConnected && SelectedBatch != null && !SelectedBatch.IsArchived;
        }

        private bool CanCompleteSchedule(MaintenanceScheduleItemViewModel? item)
        {
            return IsDatabaseConnected && item != null && item.CanComplete;
        }

        [RelayCommand(CanExecute = nameof(CanCompleteSchedule))]
        private async Task CompleteScheduleAsync(MaintenanceScheduleItemViewModel? item)
        {
            CompostBatch? batch = SelectedBatch?.Batch;
            if (item == null || !item.CanComplete || batch == null)
            {
                return;
            }

            if (item.Schedule.TaskType == TaskType.Harvest)
            {
                await HarvestAsync();
                return;
            }

            try
            {
                AppData.CompleteSchedule(batch, item.Schedule);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Gagal Memperbarui",
                    item.TaskName + " gagal ditandai selesai.\n\n" + DatabaseErrorMessage.From(ex, AppData.DatabaseInfo),
                    MessageTone.Error);
                return;
            }

            RefreshMaintenanceSchedules();
            ShowRecommendation(batch);
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
            int estimatedDays;
            if (AppData.HasRefreshedMaturityDays)
            {
                estimatedDays = AppData.CachedMaturityDays;
            }
            else
            {
                string previousStatus = DatabaseStatusText;
                DatabaseStatusText = previousStatus + "  •  Menghitung estimasi panen...";
                try
                {
                    estimatedDays = await Task.Run(AppData.RefreshEstimatedMaturityDays);
                }
                finally
                {
                    if (IsDatabaseConnected)
                    {
                        DatabaseStatusText = previousStatus;
                    }
                }
            }

            bool confirmed = await _dialogs.ConfirmAsync("Buat Batch Baru",
                $"Buat batch kompos baru mulai hari ini? Jadwal perawatan untuk {estimatedDays} hari ke depan akan dibuat otomatis.",
                isDestructive: false);
            if (!confirmed)
            {
                return;
            }

            CompostBatch batch = new CompostBatch(AppData.CurrentUser.Id, DateTime.Now, estimatedDays);
            try
            {
                AppData.BatchRepository.Add(batch);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Gagal Menyimpan", "Batch baru gagal disimpan.\n\n" + DatabaseErrorMessage.From(ex, AppData.DatabaseInfo), MessageTone.Error);
                return;
            }

            await LoadDashboardAsync(batch.Id);

            CompostBatch? savedBatch = AppData.CurrentUser.Batches.FirstOrDefault(b => b.Id == batch.Id);
            string batchName = savedBatch != null ? AppData.GetBatchName(savedBatch) : "Batch baru";
            await _dialogs.ShowMessageAsync("Berhasil", batchName + " berhasil ditambahkan ke database.", MessageTone.Success);
        }

        [RelayCommand(CanExecute = nameof(CanHarvest))]
        private async Task HarvestAsync()
        {
            CompostBatch? batch = SelectedBatch?.Batch;
            if (batch == null || batch.Status != BatchStatus.Ready)
            {
                await _dialogs.ShowMessageAsync("Panen", "Batch ini belum berstatus Siap Panen. Panen hanya bisa dilakukan setelah kompos matang.", MessageTone.Info);
                return;
            }

            string batchName = AppData.GetBatchName(batch);
            try
            {
                AppData.BatchRepository.Harvest(batch, AppData.CurrentUser.Id);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Gagal Memperbarui", batchName + " gagal diperbarui.\n\n" + DatabaseErrorMessage.From(ex, AppData.DatabaseInfo), MessageTone.Error);
                return;
            }

            await LoadDashboardAsync(batch.Id);
            await _dialogs.ShowMessageAsync("Berhasil", batchName + " berhasil dipanen.", MessageTone.Success);
        }

        [RelayCommand(CanExecute = nameof(CanDeleteBatch))]
        private async Task DeleteBatchAsync()
        {
            CompostBatch? batch = SelectedBatch?.Batch;
            if (batch == null)
            {
                await _dialogs.ShowMessageAsync("Hapus Batch", "Mohon pilih batch yang akan dihapus.", MessageTone.Info);
                return;
            }

            string batchName = AppData.GetBatchName(batch);
            if (batch.Status == BatchStatus.Archived)
            {
                await _dialogs.ShowMessageAsync("Hapus Batch",
                    batchName + " sudah dipanen dan disimpan sebagai riwayat dampak CO2e, sehingga tidak dapat dihapus.",
                    MessageTone.Info);
                return;
            }

            bool confirmed = await _dialogs.ConfirmAsync("Konfirmasi",
                "Yakin ingin menghapus " + batchName + " beserta seluruh sampah di dalamnya?",
                isDestructive: true);
            if (!confirmed)
            {
                return;
            }

            try
            {
                AppData.BatchRepository.Delete(batch, AppData.CurrentUser.Id);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowMessageAsync("Gagal Menghapus", batchName + " gagal dihapus.\n\n" + DatabaseErrorMessage.From(ex, AppData.DatabaseInfo), MessageTone.Error);
                return;
            }

            await LoadDashboardAsync(null);
            await _dialogs.ShowMessageAsync("Berhasil", batchName + " berhasil dihapus dari database.", MessageTone.Success);
        }
    }
}
