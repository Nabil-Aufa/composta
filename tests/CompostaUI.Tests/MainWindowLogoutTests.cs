
using Composta.Models;
using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class MainWindowLogoutTests
    {
        [Fact]
        public void Logout_CallsSessionServiceAndClearsDashboard()
        {
            FakeUserSessionService session = new FakeUserSessionService();
            FakeDialogService dialogs = new FakeDialogService();

            MainWindowViewModel vm = new MainWindowViewModel(
                dialogs,
                session: session);

            vm.Greeting = "Halo, Pengguna!";
            vm.TotalCo2eText = "10 kg";
            vm.TotalWeightText = "20 kg";
            vm.ActiveBatchCountText = "2";
            vm.IsDatabaseConnected = true;

            bool logoutRequested = false;
            vm.LogoutRequested += () => logoutRequested = true;

            vm.LogoutCommand.Execute(null);

            Assert.True(session.LogoutCalled);
            Assert.True(logoutRequested);
            Assert.False(vm.IsDatabaseConnected);
            Assert.Equal(string.Empty, vm.Greeting);
            Assert.Equal("-", vm.TotalCo2eText);
            Assert.Equal("-", vm.TotalWeightText);
            Assert.Equal("-", vm.ActiveBatchCountText);
            Assert.Empty(vm.Batches);
            Assert.True(vm.HasNoBatches);
        }

        private class FakeUserSessionService : IUserSessionService
        {
            public bool LogoutCalled { get; private set; }

            public Task<List<User>> GetUsersAsync()
            {
                return Task.FromResult(new List<User>());
            }

            public Task<User> RegisterUserAsync(string name, string email)
            {
                return Task.FromException<User>(
                    new NotSupportedException());
            }

            public Task LoginAsync(string userId)
            {
                return Task.CompletedTask;
            }

            public void Logout()
            {
                LogoutCalled = true;
            }
        }
    }
}
