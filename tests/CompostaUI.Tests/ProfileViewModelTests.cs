using Composta.Enums;
using Composta.Models;
using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class ProfileViewModelTests
    {
        [Fact]
        public void NewUser_HasZeroImpactAndNoBatches()
        {
            User user = new User("Nayla", "nayla@example.com");
            ProfileViewModel vm = new ProfileViewModel(user, new CarbonCalculatorService());
            Assert.Equal("Nayla", vm.Name);
            Assert.Equal("nayla@example.com", vm.Email);
            Assert.Equal("0 kg", vm.TotalWeightText);
            Assert.Equal("0 kg CO2e", vm.TotalCo2eText);
            Assert.Equal("0", vm.BatchCountText);
        }

        [Fact]
        public void Impact_IncludesHarvestedBatchesAndOnlyBelongsToSuppliedUser()
        {
            User nayla = new User("Nayla", "nayla@example.com");
            User other = new User("Other", "other@example.com");
            WasteCategory category = new WasteCategory("Sayur", 0.5, true);
            CompostBatch active = new CompostBatch();
            active.AddWasteItem(new CompostableWaste("Sayur", 2, category, BrownGreenType.Green, 60));
            CompostBatch harvested = new CompostBatch { Status = BatchStatus.Archived };
            harvested.AddWasteItem(new CompostableWaste("Buah", 4, category, BrownGreenType.Green, 60));
            nayla.AddBatch(active);
            nayla.AddBatch(harvested);

            ProfileViewModel profile = new ProfileViewModel(nayla, new CarbonCalculatorService());
            ProfileViewModel otherProfile = new ProfileViewModel(other, new CarbonCalculatorService());

            Assert.Equal("6 kg", profile.TotalWeightText);
            Assert.Equal("3 kg CO2e", profile.TotalCo2eText);
            Assert.Equal("2", profile.BatchCountText);
            Assert.Equal("0 kg CO2e", otherProfile.TotalCo2eText);
        }

        [Fact]
        public void Logout_ClearsDashboardAndRequestsLoginNavigation()
        {
            MainWindowViewModel vm = new MainWindowViewModel(new FakeDialogService());
            vm.Greeting = "Halo Nayla";
            vm.TotalWeightText = "12 kg";
            vm.TotalCo2eText = "6 kg";
            vm.ActiveBatchCountText = "2";
            vm.IsDatabaseConnected = true;
            vm.Batches.Add(new BatchItemViewModel(new CompostBatch()));
            vm.SelectedBatch = vm.Batches[0];
            bool navigated = false;
            vm.LogoutRequested += () => navigated = true;

            vm.LogoutCommand.Execute(null);

            Assert.True(navigated);
            Assert.False(AppData.IsLoggedIn);
            Assert.Empty(AppData.CurrentUser.Batches);
            Assert.Empty(vm.Batches);
            Assert.Null(vm.SelectedBatch);
            Assert.Empty(vm.Greeting);
            Assert.Equal("-", vm.TotalWeightText);
            Assert.Equal("-", vm.TotalCo2eText);
            Assert.Equal("-", vm.ActiveBatchCountText);
            Assert.False(vm.NewBatchCommand.CanExecute(null));
            Assert.False(vm.AddWasteCommand.CanExecute(null));
            Assert.False(vm.ShowProfileCommand.CanExecute(null));
        }
    }
}
