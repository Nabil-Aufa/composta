using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Composta.Models;
using Composta.Services;

namespace Composta.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserSessionService _session;
        private bool _isClosed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private User? _selectedUser;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelectProfileView))]
        [NotifyPropertyChangedFor(nameof(IsCreateProfileView))]
        private int _selectedTabIndex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadUsersCommand))]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoUsers))]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private bool _hasLoadedUsers;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNotice))]
        private string _noticeMessage = string.Empty;

        public LoginViewModel(IUserSessionService session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();
        public bool HasNoUsers => HasLoadedUsers && Users.Count == 0;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasNotice => !string.IsNullOrEmpty(NoticeMessage);
        public bool IsSelectProfileView => SelectedTabIndex == 0;
        public bool IsCreateProfileView => SelectedTabIndex == 1;
        public event Action? LoginSucceeded;

        public void Deactivate() => _isClosed = true;

        private bool CanLoadUsers() => !IsBusy && !_isClosed;
        private bool CanLogin() => CanLoadUsers() && HasLoadedUsers && SelectedUser != null;
        private bool CanRegister() => CanLoadUsers() && HasLoadedUsers;

        [RelayCommand(CanExecute = nameof(CanLoadUsers))]
        private async Task LoadUsersAsync()
        {
            if (!CanLoadUsers()) return;
            IsBusy = true;
            ErrorMessage = string.Empty;
            NoticeMessage = string.Empty;
            HasLoadedUsers = false;
            string? selectedId = SelectedUser?.Id;
            SelectedUser = null;
            Users.Clear();
            try
            {
                List<User> users = await _session.GetUsersAsync();
                if (_isClosed) return;
                foreach (User user in users) Users.Add(user);
                SelectedUser = Users.FirstOrDefault(u => u.Id == selectedId);
                HasLoadedUsers = true;
                if (Users.Count == 0) SelectedTabIndex = 1;
            }
            catch (Exception ex)
            {
                ErrorMessage = DatabaseErrorMessage.From(ex, AppData.DatabaseInfo);
            }
            finally
            {
                OnPropertyChanged(nameof(HasNoUsers));
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (!CanLogin()) return;
            string userId = SelectedUser!.Id;
            await RunBusyAsync(async () =>
            {
                await _session.LoginAsync(userId);
                if (!_isClosed) LoginSucceeded?.Invoke();
            });
        }

        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task RegisterAsync()
        {
            if (!CanRegister()) return;
            ErrorMessage = UserValidator.Validate(Name, Email) ?? string.Empty;
            NoticeMessage = string.Empty;
            if (HasError) return;

            string name = Name.Trim();
            string email = UserValidator.NormalizeEmail(Email);
            await RunBusyAsync(async () =>
            {
                User user = await _session.RegisterUserAsync(name, email);
                if (_isClosed) return;

                Users.Add(user);
                OnPropertyChanged(nameof(HasNoUsers));
                SelectedUser = user;
                Name = string.Empty;
                Email = string.Empty;
                SelectedTabIndex = 0;
                NoticeMessage = "Profil berhasil dibuat dan tersedia pada daftar profil.";
                await _session.LoginAsync(user.Id);
                if (!_isClosed) LoginSucceeded?.Invoke();
            });
        }

        [RelayCommand]
        private void ShowCreateProfile()
        {
            ErrorMessage = string.Empty;
            NoticeMessage = string.Empty;
            SelectedTabIndex = 1;
        }

        [RelayCommand]
        private void ShowSelectProfile()
        {
            ErrorMessage = string.Empty;
            SelectedTabIndex = 0;
        }

        private async Task RunBusyAsync(Func<Task> operation)
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex is ArgumentException
                    ? ex.Message
                    : DatabaseErrorMessage.From(ex, AppData.DatabaseInfo);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
