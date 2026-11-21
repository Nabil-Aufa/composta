using Composta.Models;
using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class LoginViewModelTests
    {
        [Fact]
        public async Task Load_ListsProfilesWithoutAutomaticallyLoggingIn()
        {
            FakeSession session = new FakeSession();
            session.Users.Add(new User("Nayla", "nayla@example.com"));
            LoginViewModel vm = new LoginViewModel(session);

            await vm.LoadUsersCommand.ExecuteAsync(null);

            Assert.Single(vm.Users);
            Assert.Null(vm.SelectedUser);
            Assert.False(vm.LoginCommand.CanExecute(null));
            Assert.Equal(0, session.LoginCalls);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task EmptyDatabase_ShowsCreateProfileAndAllowsRegistration()
        {
            LoginViewModel vm = new LoginViewModel(new FakeSession());
            await vm.LoadUsersCommand.ExecuteAsync(null);

            Assert.True(vm.HasNoUsers);
            Assert.Equal(1, vm.SelectedTabIndex);
            Assert.True(vm.RegisterCommand.CanExecute(null));
        }

        [Theory]
        [InlineData("", "a@example.com", "Nama wajib")]
        [InlineData("   ", "a@example.com", "Nama wajib")]
        [InlineData("Nayla", "", "Email wajib")]
        [InlineData("Nayla", "  ", "Email wajib")]
        [InlineData("Nayla", "invalid", "Format email")]
        public async Task InvalidRegistration_ShowsErrorWithoutWriting(string name, string email, string expected)
        {
            FakeSession session = new FakeSession();
            LoginViewModel vm = new LoginViewModel(session);
            await vm.LoadUsersCommand.ExecuteAsync(null);
            vm.Name = name;
            vm.Email = email;

            await vm.RegisterCommand.ExecuteAsync(null);

            Assert.Contains(expected, vm.ErrorMessage);
            Assert.Equal(0, session.RegisterCalls);
            Assert.Equal(0, session.LoginCalls);
        }

        [Fact]
        public async Task SelectProfile_LogsInSelectedIdBeforeNavigation()
        {
            FakeSession session = new FakeSession();
            session.Users.Add(new User("Nabil", "nabil@example.com"));
            session.Users.Add(new User("Nayla", "nayla@example.com"));
            LoginViewModel vm = new LoginViewModel(session);
            await vm.LoadUsersCommand.ExecuteAsync(null);
            vm.SelectedUser = vm.Users[1];
            string? idWhenNavigated = null;
            vm.LoginSucceeded += () => idWhenNavigated = session.LoggedInId;

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.Equal(vm.Users[1].Id, idWhenNavigated);
            Assert.Equal(1, session.LoginCalls);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task Register_NormalizesInputAndLogsInNewProfile()
        {
            FakeSession session = new FakeSession();
            LoginViewModel vm = new LoginViewModel(session);
            await vm.LoadUsersCommand.ExecuteAsync(null);
            vm.Name = "  Nayla  ";
            vm.Email = "  NAYLA@Example.COM  ";
            bool navigated = false;
            vm.LoginSucceeded += () => navigated = true;

            await vm.RegisterCommand.ExecuteAsync(null);

            User user = Assert.Single(vm.Users);
            Assert.Equal("Nayla", user.Name);
            Assert.Equal("nayla@example.com", user.Email);
            Assert.Equal(user.Id, session.LoggedInId);
            Assert.True(navigated);
            Assert.False(vm.HasNoUsers);
            Assert.Empty(vm.Name);
            Assert.Empty(vm.Email);
        }

        [Fact]
        public async Task DuplicateEmail_ShowsErrorAndStaysOnLoginWindow()
        {
            FakeSession session = new FakeSession
            {
                RegisterFailure = new InvalidOperationException("Email sudah terdaftar.")
            };
            LoginViewModel vm = new LoginViewModel(session);
            await vm.LoadUsersCommand.ExecuteAsync(null);
            vm.Name = "Nayla";
            vm.Email = "nayla@example.com";
            bool navigated = false;
            vm.LoginSucceeded += () => navigated = true;

            await vm.RegisterCommand.ExecuteAsync(null);

            Assert.Contains("sudah terdaftar", vm.ErrorMessage);
            Assert.False(navigated);
            Assert.Equal(0, session.LoginCalls);
            Assert.True(vm.RegisterCommand.CanExecute(null));
        }

        [Fact]
        public async Task LoadFailure_DisablesLoginAndRegistrationUntilReloadSucceeds()
        {
            FakeSession session = new FakeSession { LoadFailure = new TimeoutException() };
            LoginViewModel vm = new LoginViewModel(session);

            await vm.LoadUsersCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.False(vm.HasLoadedUsers);
            Assert.False(vm.LoginCommand.CanExecute(null));
            Assert.False(vm.RegisterCommand.CanExecute(null));
            Assert.True(vm.LoadUsersCommand.CanExecute(null));

            session.LoadFailure = null;
            await vm.LoadUsersCommand.ExecuteAsync(null);
            Assert.True(vm.HasLoadedUsers);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task SavedProfile_LoginFailureCanRetryWithoutDuplicateRegistration()
        {
            FakeSession session = new FakeSession { LoginFailure = new TimeoutException() };
            LoginViewModel vm = new LoginViewModel(session);
            await vm.LoadUsersCommand.ExecuteAsync(null);
            vm.Name = "Nayla";
            vm.Email = "nayla@example.com";
            bool navigated = false;
            vm.LoginSucceeded += () => navigated = true;

            await vm.RegisterCommand.ExecuteAsync(null);

            Assert.False(navigated);
            Assert.True(vm.HasError);
            Assert.True(vm.HasNotice);
            Assert.Same(Assert.Single(vm.Users), vm.SelectedUser);
            Assert.Equal(0, vm.SelectedTabIndex);

            session.LoginFailure = null;
            await vm.LoginCommand.ExecuteAsync(null);

            Assert.True(navigated);
            Assert.Equal(1, session.RegisterCalls);
            Assert.Equal(2, session.LoginCalls);
        }

        [Fact]
        public async Task BusyLogin_DisablesAllActionsAndClosedWindowCannotNavigate()
        {
            TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            FakeSession session = new FakeSession { PendingLogin = completion.Task };
            session.Users.Add(new User("Nayla", "nayla@example.com"));
            LoginViewModel vm = new LoginViewModel(session);
            await vm.LoadUsersCommand.ExecuteAsync(null);
            vm.SelectedUser = vm.Users[0];
            bool navigated = false;
            vm.LoginSucceeded += () => navigated = true;

            Task login = vm.LoginCommand.ExecuteAsync(null);
            Assert.True(vm.IsBusy);
            Assert.False(vm.LoginCommand.CanExecute(null));
            Assert.False(vm.RegisterCommand.CanExecute(null));
            Assert.False(vm.LoadUsersCommand.CanExecute(null));

            vm.Deactivate();
            completion.SetResult();
            await login;
            Assert.False(navigated);
            Assert.False(vm.IsBusy);
            Assert.False(vm.LoginCommand.CanExecute(null));
        }

        private sealed class FakeSession : IUserSessionService
        {
            public List<User> Users { get; } = new List<User>();
            public Exception? LoadFailure { get; set; }
            public Exception? RegisterFailure { get; set; }
            public Exception? LoginFailure { get; set; }
            public Task? PendingLogin { get; set; }
            public int RegisterCalls { get; private set; }
            public int LoginCalls { get; private set; }
            public string? LoggedInId { get; private set; }

            public Task<List<User>> GetUsersAsync() => LoadFailure != null
                ? Task.FromException<List<User>>(LoadFailure)
                : Task.FromResult(Users.ToList());

            public Task<User> RegisterUserAsync(string name, string email)
            {
                RegisterCalls++;
                if (RegisterFailure != null) return Task.FromException<User>(RegisterFailure);
                User user = new User(name, email);
                Users.Add(user);
                return Task.FromResult(user);
            }

            public async Task LoginAsync(string userId)
            {
                LoginCalls++;
                if (PendingLogin != null) await PendingLogin;
                if (LoginFailure != null) throw LoginFailure;
                LoggedInId = userId;
            }

            public void Logout() => LoggedInId = null;
        }
    }
}
