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

        private async Task<MainWindowViewModel> LoadedViewModel()
        {
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
            Assert.Same(vm.Batches[0], vm.SelectedBatch);
            Assert.True(vm.SelectedBatch?.IsArchived);
            Assert.Equal("Kompos dari batch ini sudah dipanen.", vm.RecommendationText);
            Assert.Empty(_dialogs.Messages);
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

            vm.SelectedBatch = null;
            Assert.Equal("Belum ada batch yang dipilih.", vm.RecommendationText);
            Assert.False(vm.DeleteBatchCommand.CanExecute(null));
        }

        [SkippableFact]
        public async Task NewBatch_IsSavedAndSelected()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            await vm.NewBatchCommand.ExecuteAsync(null);

            Assert.Equal(5, vm.Batches.Count);
            Assert.Equal("Batch 5", vm.SelectedBatch?.Name);
            Assert.True(vm.SelectedBatch?.IsActive);
            Assert.Contains("Berhasil: Batch 5 berhasil ditambahkan ke database.", _dialogs.Messages);
            Assert.Equal(6, Db.Count("SELECT COUNT(*) FROM compost_batches"));
        }

        [SkippableFact]
        public async Task Harvest_ArchivesReadyBatch()
        {
            RequireDatabase();

            MainWindowViewModel vm = await LoadedViewModel();
            BatchItemViewModel ready = vm.Batches.Single(b => b.IsReady);
            vm.SelectedBatch = ready;

            await vm.HarvestCommand.ExecuteAsync(null);

            Assert.True(vm.SelectedBatch?.IsArchived);
            Assert.Equal("2", vm.ActiveBatchCountText);
            Assert.Equal(2, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE status = 'Archived'"));
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
    }
}
