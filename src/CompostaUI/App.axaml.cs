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
                MainWindow window = new MainWindow();
                MainWindowViewModel viewModel = new MainWindowViewModel(new DialogService(window));
                window.DataContext = viewModel;
                window.Opened += async (sender, e) => await viewModel.LoadAsync();
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
