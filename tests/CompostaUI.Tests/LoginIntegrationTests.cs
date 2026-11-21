using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class LoginIntegrationTests : DatabaseTest
    {
        public LoginIntegrationTests(DatabaseFixture db) : base(db) { }

        [SkippableFact]
        public async Task LoginAndSwitchUser_RefreshesDashboardAndProfileFromPostgres()
        {
            RequireDatabase();
            UserSessionService session = new UserSessionService();
            LoginViewModel login = new LoginViewModel(session);
            await login.LoadUsersCommand.ExecuteAsync(null);
            Assert.False(AppData.IsLoggedIn);
            login.SelectedUser = login.Users.Single(u => u.Id == DatabaseFixture.NabilId);
            await login.LoginCommand.ExecuteAsync(null);

            FakeDialogService firstDialogs = new FakeDialogService();
            MainWindowViewModel first = new MainWindowViewModel(firstDialogs);
            await first.LoadAsync();
            await first.ShowProfileCommand.ExecuteAsync(null);

            Assert.Contains("Nabil", first.Greeting);
            Assert.Equal(4, first.Batches.Count);
            Assert.Equal("4", firstDialogs.LastProfile?.BatchCountText);
            Assert.Equal(first.TotalWeightText, firstDialogs.LastProfile?.TotalWeightText);

            first.LogoutCommand.Execute(null);
            Assert.Empty(first.Batches);
            Assert.False(AppData.IsLoggedIn);

            LoginViewModel nextLogin = new LoginViewModel(session);
            await nextLogin.LoadUsersCommand.ExecuteAsync(null);
            Assert.Null(nextLogin.SelectedUser);
            nextLogin.SelectedUser = nextLogin.Users.Single(u => u.Id == DatabaseFixture.DemoUserId);
            await nextLogin.LoginCommand.ExecuteAsync(null);

            FakeDialogService secondDialogs = new FakeDialogService();
            MainWindowViewModel second = new MainWindowViewModel(secondDialogs);
            await second.LoadAsync();
            await second.ShowProfileCommand.ExecuteAsync(null);

            Assert.Equal(DatabaseFixture.DemoUserId, AppData.CurrentUser.Id);
            Assert.Single(second.Batches);
            Assert.All(second.Batches, b => Assert.Equal(DatabaseFixture.DemoUserId, b.Batch.UserId));
            Assert.Equal(AppData.CurrentUser.Name, secondDialogs.LastProfile?.Name);
            Assert.Equal("1", secondDialogs.LastProfile?.BatchCountText);
            Assert.NotEqual(firstDialogs.LastProfile?.TotalWeightText, secondDialogs.LastProfile?.TotalWeightText);
        }

        [SkippableFact]
        public async Task RegisterProfile_CreatesRealUserAndOpensEmptyDashboard()
        {
            RequireDatabase();
            LoginViewModel login = new LoginViewModel(new UserSessionService());
            await login.LoadUsersCommand.ExecuteAsync(null);
            login.Name = "  Nayla  ";
            login.Email = "  NAYLA@EXAMPLE.COM  ";
            bool navigated = false;
            login.LoginSucceeded += () => navigated = true;

            await login.RegisterCommand.ExecuteAsync(null);

            Assert.True(navigated);
            Assert.False(login.HasError);
            Assert.Equal("nayla@example.com", AppData.CurrentUser.Email);
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM users WHERE email = 'nayla@example.com'"));
            FakeDialogService dialogs = new FakeDialogService();
            MainWindowViewModel dashboard = new MainWindowViewModel(dialogs);
            await dashboard.LoadAsync();
            await dashboard.ShowProfileCommand.ExecuteAsync(null);
            Assert.Contains("Nayla", dashboard.Greeting);
            Assert.Empty(dashboard.Batches);
            Assert.Equal("0 kg", dialogs.LastProfile?.TotalWeightText);
            Assert.Equal("0 kg CO2e", dialogs.LastProfile?.TotalCo2eText);
            Assert.Equal("0", dialogs.LastProfile?.BatchCountText);
        }
    }
}
