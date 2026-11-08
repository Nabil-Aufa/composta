using System.Globalization;
using Avalonia;

namespace Composta
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("id-ID");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("id-ID");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
