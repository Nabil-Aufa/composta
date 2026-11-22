using Composta.Enums;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class MainWindowViewModelTests : DatabaseTest
    {
        private readonly FakeDialogService _dialogs = new FakeDialogService();

        public MainWindowViewModelTests(DatabaseFixture db) : base(db)
        {
        }

        private const string ReadyBatchId = "b8d0e6f2-4a1b-4c2d-9e3f-000000000001";

        private void AddPendingSchedulesToReadyBatch()
        {
            using var conn = new Npgsql.NpgsqlConnection(Db.Connstring);
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(
                "INSERT INTO maintenance_schedules (id, batch_id, task_type, due_date, is_completed) VALUES " +
                "('d2f4b6c8-3e5a-4b7c-9d1e-000000000091', '" + ReadyBatchId + "', 'Water', NOW() - INTERVAL '6 days', FALSE), " +
                "('d2f4b6c8-3e5a-4b7c-9d1e-000000000092', '" + ReadyBatchId + "', 'Harvest', NOW() - INTERVAL '5 days', FALSE)", conn);
            cmd.ExecuteNonQuery();
        }

        private async Task<MainWindowViewModel> LoadedViewModel()
        {
            AppData.Login(DatabaseFixture.NabilId);
            MainWindowViewModel vm = new MainWindowViewModel(_dialogs);
            await vm.LoadAsync();
            return vm;
        }

        [SkippableFact]
        public async Task Load_FillsDashboardForActiveUser()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();

            Assert.True(vm.IsDatabaseConnected);
            Assert.Contains("Nabil Aufa Danaputra", vm.Greeting);
            Assert.Equal(4, vm.Batches.Count);
            Assert.Equal("30,5 kg", vm.TotalWeightText);
            Assert.Equal("3", vm.ActiveBatchCountText);
            Assert.False(vm.HasNoBatches);
            Assert.True(vm.Batches[0].IsArchived);
            Assert.Same(vm.Batches[1], vm.SelectedBatch);
            Assert.Equal("Rekomendasi Batch 2", vm.RecommendationTitle);
            Assert.Contains(
            _dialogs.Messages,
            message => message.StartsWith("Pengingat Perawatan:")
                && message.Contains("Terlambat:"));
        }

        [SkippableFact]
        public async Task SelectingBatch_UpdatesRecommendationAndCommands()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel ready = vm.Batches.Single(b => b.IsReady);
            BatchItemViewModel active = vm.Batches.Single(b => b.IsActive);

            vm.SelectedBatch = ready;
            Assert.Equal("Rekomendasi " + ready.Name, vm.RecommendationTitle);
            Assert.True(vm.IsHighPriority);
            Assert.True(vm.HarvestCommand.CanExecute(null));

            vm.SelectedBatch = active;
            Assert.Contains("terlalu rendah", vm.RecommendationText);
            Assert.False(vm.HarvestCommand.CanExecute(null));
            Assert.True(vm.DeleteBatchCommand.CanExecute(null));

            vm.SelectedBatch = vm.Batches.Single(b => b.IsArchived);
            Assert.Equal("Kompos dari batch ini sudah dipanen.", vm.RecommendationText);
            Assert.True(vm.IsLowPriority);
            Assert.False(vm.HarvestCommand.CanExecute(null));
            Assert.False(vm.DeleteBatchCommand.CanExecute(null));

            vm.SelectedBatch = null;
            Assert.Equal("Belum ada batch yang dipilih.", vm.RecommendationText);
            Assert.False(vm.DeleteBatchCommand.CanExecute(null));
        }

        [SkippableFact]
        public async Task CompleteSchedule_SavesStatusAndUpdatesRecommendation()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            vm.SelectedBatch = vm.Batches.Single(b => b.Batch.Schedules.Any(s => s.IsOverdue()));
            MaintenanceScheduleItemViewModel overdue = vm.MaintenanceSchedules.Single(s => s.IsOverdue);
            Assert.Contains("jadwal perawatan yang terlewat", vm.RecommendationText);
            Assert.True(vm.CompleteScheduleCommand.CanExecute(overdue));

            await vm.CompleteScheduleCommand.ExecuteAsync(overdue);

            MaintenanceScheduleItemViewModel done = vm.MaintenanceSchedules.Single(s => s.Schedule.Id == overdue.Schedule.Id);
            Assert.True(done.IsCompleted);
            Assert.Equal("Selesai", done.StatusText);
            Assert.False(vm.CompleteScheduleCommand.CanExecute(done));
            Assert.DoesNotContain("jadwal perawatan yang terlewat", vm.RecommendationText);
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM maintenance_schedules WHERE id = '"
                + overdue.Schedule.Id + "' AND is_completed"));
        }

        [SkippableFact]
        public async Task CompleteSchedule_IsDisabledWithoutDatabase()
        {
            MainWindowViewModel vm = new MainWindowViewModel(_dialogs);
            MaintenanceScheduleItemViewModel item = new MaintenanceScheduleItemViewModel(
                new Composta.Models.MaintenanceSchedule(TaskType.Turn, DateTime.Today));

            Assert.False(vm.CompleteScheduleCommand.CanExecute(item));
            Assert.False(vm.CompleteScheduleCommand.CanExecute(null));
            Assert.True(vm.HasNoMaintenanceSchedules);
            Assert.Contains("batch kompos yang dipilih", vm.MaintenanceEmptyText);
        }

        [SkippableFact]
        public async Task NewBatch_IsCancelledWithoutConfirmation()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            _dialogs.ConfirmResult = false;

            await vm.NewBatchCommand.ExecuteAsync(null);

            Assert.Equal(4, vm.Batches.Count);
            Assert.Equal(5, Db.Count("SELECT COUNT(*) FROM compost_batches"));
            Assert.Contains(_dialogs.Messages, m => m.StartsWith("Buat Batch Baru:"));
            Assert.False(_dialogs.DestructiveByTitle["Buat Batch Baru"]);
            Assert.DoesNotContain(_dialogs.Messages, m => m.StartsWith("Berhasil:"));
        }

        [SkippableFact]
        public async Task CompleteSchedule_AlsoCompletesEarlierSchedulesOfSameTask()
        {
            RequireDatabase();

            const string activeBatchId = "b8d0e6f2-4a1b-4c2d-9e3f-000000000003";
            using (var conn = new Npgsql.NpgsqlConnection(Db.Connstring))
            {
                conn.Open();
                using var cmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO maintenance_schedules (id, batch_id, task_type, due_date, is_completed) VALUES " +
                    "('d2f4b6c8-3e5a-4b7c-9d1e-000000000081', '" + activeBatchId + "', 'Water', NOW() - INTERVAL '8 days', FALSE), " +
                    "('d2f4b6c8-3e5a-4b7c-9d1e-000000000082', '" + activeBatchId + "', 'Water', NOW() - INTERVAL '1 days', FALSE), " +
                    "('d2f4b6c8-3e5a-4b7c-9d1e-000000000083', '" + activeBatchId + "', 'Turn', NOW() - INTERVAL '3 days', FALSE), " +
                    "('d2f4b6c8-3e5a-4b7c-9d1e-000000000084', '" + activeBatchId + "', 'Water', NOW() + INTERVAL '6 days', FALSE)", conn);
                cmd.ExecuteNonQuery();
            }

            MainWindowViewModel vm = await LoadedViewModel();
            vm.SelectedBatch = vm.Batches.Single(b => b.Batch.Id == activeBatchId);
            MaintenanceScheduleItemViewModel latestWater = vm.MaintenanceSchedules
                .Single(s => s.Schedule.Id == "d2f4b6c8-3e5a-4b7c-9d1e-000000000082");

            await vm.CompleteScheduleCommand.ExecuteAsync(latestWater);

            Assert.Equal(2, Db.Count("SELECT COUNT(*) FROM maintenance_schedules WHERE batch_id = '"
                + activeBatchId + "' AND task_type = 'Water' AND is_completed"));
            Assert.Equal(2, Db.Count("SELECT COUNT(*) FROM maintenance_schedules WHERE id IN "
                + "('d2f4b6c8-3e5a-4b7c-9d1e-000000000083', 'd2f4b6c8-3e5a-4b7c-9d1e-000000000084') AND NOT is_completed"));
            Assert.True(vm.MaintenanceSchedules.Single(s => s.Schedule.Id.EndsWith("081")).IsCompleted);
            Assert.False(vm.MaintenanceSchedules.Single(s => s.Schedule.Id.EndsWith("083")).IsCompleted);
            Assert.False(vm.MaintenanceSchedules.Single(s => s.Schedule.Id.EndsWith("084")).IsCompleted);
        }

        [SkippableFact]
        public async Task NewBatch_IsSavedAndSelected()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            await vm.NewBatchCommand.ExecuteAsync(null);
            Assert.False(_dialogs.DestructiveByTitle["Buat Batch Baru"]);

            Assert.Equal(5, vm.Batches.Count);
            Assert.Equal("Batch 5", vm.SelectedBatch?.Name);
            Assert.True(vm.SelectedBatch?.IsActive);
            Assert.Contains("Berhasil: Batch 5 berhasil ditambahkan ke database.", _dialogs.Messages);
            Assert.Equal(MessageTone.Success, _dialogs.TonesByTitle["Berhasil"]);
            Assert.Equal(6, Db.Count("SELECT COUNT(*) FROM compost_batches"));
        }

        [SkippableFact]
        public async Task Harvest_ArchivesReadyBatch()
        {
            RequireDatabase();
            AddPendingSchedulesToReadyBatch();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel ready = vm.Batches.Single(b => b.IsReady);
            vm.SelectedBatch = ready;

            await vm.HarvestCommand.ExecuteAsync(null);

            Assert.True(vm.SelectedBatch?.IsArchived);
            Assert.Equal("2", vm.ActiveBatchCountText);
            Assert.Equal(2, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE status = 'Archived'"));
            Assert.Equal(2, vm.MaintenanceSchedules.Count);
            Assert.All(vm.MaintenanceSchedules, s => Assert.True(s.IsCompleted));
            Assert.Equal(0, Db.Count("SELECT COUNT(*) FROM maintenance_schedules WHERE batch_id = '"
                + ready.Batch.Id + "' AND NOT is_completed"));
        }

        [SkippableFact]
        public async Task CompletingHarvestSchedule_HarvestsReadyBatch()
        {
            RequireDatabase();
            AddPendingSchedulesToReadyBatch();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel ready = vm.Batches.Single(b => b.IsReady);
            string batchId = ready.Batch.Id;
            vm.SelectedBatch = ready;
            MaintenanceScheduleItemViewModel harvest = vm.MaintenanceSchedules
                .Single(s => s.Schedule.TaskType == TaskType.Harvest);

            await vm.CompleteScheduleCommand.ExecuteAsync(harvest);

            Assert.True(vm.Batches.Single(b => b.Batch.Id == batchId).IsArchived);
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE id = '"
                + batchId + "' AND status = 'Archived'"));
            Assert.Contains(_dialogs.Messages, m => m.EndsWith("berhasil dipanen."));
        }

        [SkippableFact]
        public async Task CompletingHarvestSchedule_OfUnreadyBatch_DoesNotHarvest()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel active = vm.Batches.Single(b => b.IsActive);
            vm.SelectedBatch = active;
            MaintenanceScheduleItemViewModel harvest = new MaintenanceScheduleItemViewModel(
                new Composta.Models.MaintenanceSchedule(TaskType.Harvest, DateTime.Today));

            await vm.CompleteScheduleCommand.ExecuteAsync(harvest);

            Assert.True(active.IsActive);
            Assert.Contains(_dialogs.Messages, m => m.StartsWith("Panen:"));
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE status = 'Archived'"));
        }

        [SkippableFact]
        public async Task DeleteBatch_RespectsConfirmation()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel target = vm.Batches.Single(b => b.IsCuring);
            vm.SelectedBatch = target;
            string itemsOfTarget = "SELECT COUNT(*) FROM waste_items WHERE batch_id = '" + target.Batch.Id + "'";

            _dialogs.ConfirmResult = false;
            await vm.DeleteBatchCommand.ExecuteAsync(null);
            Assert.Equal(4, vm.Batches.Count);
            Assert.Equal(2, Db.Count(itemsOfTarget));

            _dialogs.ConfirmResult = true;
            await vm.DeleteBatchCommand.ExecuteAsync(null);
            Assert.Equal(3, vm.Batches.Count);
            Assert.DoesNotContain(vm.Batches, b => b.Batch.Id == target.Batch.Id);
            Assert.Equal(0, Db.Count(itemsOfTarget));
            Assert.True(_dialogs.DestructiveByTitle["Konfirmasi"]);
        }

        [SkippableFact]
        public async Task DeleteBatch_KeepsHarvestedBatchAndCo2eHistory()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel harvested = vm.Batches.Single(b => b.IsArchived);
            string totalBefore = vm.TotalCo2eText;
            vm.SelectedBatch = harvested;
            _dialogs.ConfirmResult = true;

            await vm.DeleteBatchCommand.ExecuteAsync(null);

            Assert.Equal(4, vm.Batches.Count);
            Assert.Equal(totalBefore, vm.TotalCo2eText);
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE id = '" + harvested.Batch.Id + "'"));
            Assert.Contains(_dialogs.Messages, m => m.StartsWith("Hapus Batch:") && m.Contains("tidak dapat dihapus"));
            Assert.DoesNotContain(_dialogs.Messages, m => m.StartsWith("Konfirmasi:"));
        }

        [SkippableFact]
        public async Task Harvest_DisablesDeleteForHarvestedBatch()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            vm.SelectedBatch = vm.Batches.Single(b => b.IsReady);
            Assert.True(vm.DeleteBatchCommand.CanExecute(null));

            await vm.HarvestCommand.ExecuteAsync(null);

            Assert.True(vm.SelectedBatch?.IsArchived);
            Assert.False(vm.DeleteBatchCommand.CanExecute(null));
        }

        [SkippableFact]
        public async Task AddWaste_SavesItemAndReloadsDashboard()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            vm.SelectedBatch = vm.Batches.Single(b => b.IsActive);
            _dialogs.OnAddWaste = addWaste =>
            {
                bool closedWithSave = false;
                addWaste.CloseRequested += saved => closedWithSave = saved;
                addWaste.WasteName = "Daun mangga";
                addWaste.SelectedCategory = addWaste.Categories.First(c => c.Name.StartsWith("Daun"));
                addWaste.IsBrown = true;
                addWaste.Weight = 3;
                addWaste.SaveCommand.Execute(null);
                return closedWithSave;
            };

            await vm.AddWasteCommand.ExecuteAsync(null);

            Assert.Equal("33,5 kg", vm.TotalWeightText);
            Assert.True(vm.SelectedBatch?.IsActive);
            Assert.Contains("rasio brown/green 1,2", vm.SelectedBatch?.Detail);
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM waste_items WHERE name = 'Daun mangga' AND brown_green_type = 'Brown'"));
        }

        [SkippableFact]
        public async Task Load_UpdatesBatchStatusInDatabase()
        {
            RequireDatabase();

            using (Npgsql.NpgsqlConnection conn = new Npgsql.NpgsqlConnection(Db.Connstring))
            {
                conn.Open();
                using Npgsql.NpgsqlCommand cmd = new Npgsql.NpgsqlCommand(
                    "UPDATE compost_batches SET status = 'Active' WHERE id = 'b8d0e6f2-4a1b-4c2d-9e3f-000000000002'", conn);
                cmd.ExecuteNonQuery();
            }

            MainWindowViewModel vm = await LoadedViewModel();

            Assert.Equal(BatchStatus.Curing, vm.Batches.Single(b => b.Batch.Id == "b8d0e6f2-4a1b-4c2d-9e3f-000000000002").Batch.Status);
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE id = 'b8d0e6f2-4a1b-4c2d-9e3f-000000000002' AND status = 'Curing'"));
        }
        [SkippableFact]
        public async Task Load_ShowsMaintenanceReminderWhenSchedulesAreOverdue()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();

            Assert.True(vm.IsDatabaseConnected);
            Assert.Contains(
                _dialogs.Messages,
                message => message.StartsWith("Pengingat Perawatan:")
                    && message.Contains("Terlambat:"));
            Assert.Equal(MessageTone.Warning, _dialogs.TonesByTitle["Pengingat Perawatan"]);
        }

        [SkippableFact]
        public async Task Load_IgnoresOverdueSchedulesOfHarvestedBatch()
        {
            RequireDatabase();

            using (var conn = new Npgsql.NpgsqlConnection(Db.Connstring))
            {
                conn.Open();
                using var cmd = new Npgsql.NpgsqlCommand(
                    "UPDATE maintenance_schedules SET is_completed = TRUE; " +
                    "INSERT INTO maintenance_schedules (id, batch_id, task_type, due_date, is_completed) " +
                    "VALUES ('d2f4b6c8-3e5a-4b7c-9d1e-000000000099', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000005', " +
                    "'Water', NOW() - INTERVAL '30 days', FALSE)", conn);
                cmd.ExecuteNonQuery();
            }

            MainWindowViewModel vm = await LoadedViewModel();

            Assert.True(vm.IsDatabaseConnected);
            Assert.True(vm.Batches[0].IsArchived);
            Assert.DoesNotContain(
                _dialogs.Messages,
                message => message.StartsWith("Pengingat Perawatan:"));
        }

        [SkippableFact]
        public async Task Load_DoesNotShowReminderWhenAllSchedulesAreCompleted()
        {
            RequireDatabase();

            using (var conn = new Npgsql.NpgsqlConnection(Db.Connstring))
            {
                conn.Open();
                using var cmd = new Npgsql.NpgsqlCommand(
                    "UPDATE maintenance_schedules SET is_completed = TRUE", conn);
                cmd.ExecuteNonQuery();
            }

            MainWindowViewModel vm = await LoadedViewModel();

            Assert.True(vm.IsDatabaseConnected);
            Assert.DoesNotContain(
                _dialogs.Messages,
                message => message.StartsWith("Pengingat Perawatan:"));
        }
    }
}
