using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Composta.Services;
using Composta.ViewModels;
using Composta.Views;

namespace Composta
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = CreateLoginWindow(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static LoginWindow CreateLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            LoginWindow window = new LoginWindow();
            LoginViewModel viewModel = new LoginViewModel(new UserSessionService());
            window.DataContext = viewModel;
            window.Opened += async (sender, e) => await viewModel.LoadUsersCommand.ExecuteAsync(null);
            window.Closed += (sender, e) => viewModel.Deactivate();
            viewModel.LoginSucceeded += () => ReplaceWindow(desktop, CreateDashboardWindow(desktop));
            return window;
        }

        private static MainWindow CreateDashboardWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow window = new MainWindow();
            MainWindowViewModel viewModel = new MainWindowViewModel(new DialogService(window));
            window.DataContext = viewModel;
            window.Opened += async (sender, e) => await viewModel.LoadAsync();
            viewModel.LogoutRequested += () => ReplaceWindow(desktop, CreateLoginWindow(desktop));
            return window;
        }

        private static void ReplaceWindow(IClassicDesktopStyleApplicationLifetime desktop, Avalonia.Controls.Window next)
        {
            Avalonia.Controls.Window? previous = desktop.MainWindow;
            desktop.MainWindow = next;
            next.Show();
            previous?.Close();
        }
    }
}
