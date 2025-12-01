using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using NHotkey;
using NHotkey.Wpf;
using TouchGrass.Services;

namespace TouchGrass
{
    public partial class App : Application
    {
        private TaskbarIcon? _notifyIcon;
        private static System.Threading.Mutex? _mutex;
        public static SettingsService SettingsService { get; private set; } = new SettingsService();
        public static GameService GameService { get; private set; } = new GameService();

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "TouchGrass_SingleInstance_Mutex";
            bool createdNew;

            _mutex = new System.Threading.Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            try
            {
                // Initialize the TaskbarIcon
                _notifyIcon = (TaskbarIcon)FindResource("TrayIcon");
                UpdateTrayIconVisibility();

                // Register Hotkey
                RegisterHotkey();

                // Defer MainWindow creation to improve startup performance
                Dispatcher.InvokeAsync(() =>
                {
                    if (MainWindow == null)
                    {
                        new MainWindow();
                    }
                    // Ensure it's hidden initially
                    MainWindow?.Hide();
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crash_log.txt", $"Startup Error: {ex}\n");
                MessageBox.Show($"Startup Error: {ex.Message}");
                Shutdown();
            }
        }

        public void RegisterHotkey()
        {
            try
            {
                var settings = SettingsService.CurrentSettings;
                HotkeyManager.Current.AddOrReplace("ToggleWindow", settings.HotkeyKey, settings.HotkeyModifiers, OnToggleWindow);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crash_log.txt", $"Hotkey Error: {ex}\n");
            }
        }

        public void UpdateTrayIconVisibility()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visibility = SettingsService.CurrentSettings.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnToggleWindow(object? sender, HotkeyEventArgs e)
        {
            ToggleWindow();
            e.Handled = true;
        }

        private void ToggleWindow()
        {
            if (MainWindow == null) return;

            if (MainWindow.IsVisible)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Activate();
                }
                else
                {
                    MainWindow.Hide();
                }
            }
            else
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Views.SettingsWindow(SettingsService, GameService);
            if (settingsWindow.ShowDialog() == true)
            {
                RegisterHotkey();
                UpdateTrayIconVisibility();
            }
        }

        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            _notifyIcon?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
