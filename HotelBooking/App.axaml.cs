using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;

namespace HotelBooking
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            try
            {
                Console.WriteLine("Loading XAML for App...");
                AvaloniaXamlLoader.Load(this);
                Console.WriteLine("XAML loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XAML load failed: {ex}");
                throw;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Для Avalonia 11.x используем только это:
            RequestedThemeVariant = ThemeVariant.Light;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}