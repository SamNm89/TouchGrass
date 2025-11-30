using System;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Win32;

namespace TouchGrass.Services
{
    public class AppSettings
    {
        public Key HotkeyKey { get; set; } = Key.Space;
        public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Alt;
        public bool RunOnStartup { get; set; } = false;
        public bool ShowTrayIcon { get; set; } = true;
    }

    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private const string AppName = "TouchGrass";

        public AppSettings CurrentSettings { get; private set; }

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appData, "GhostLauncher");
            _settingsFilePath = Path.Combine(folderPath, "settings.json");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            CurrentSettings = LoadSettings();
        }

        private AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            CurrentSettings = settings;
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);

                SetRunOnStartup(settings.RunOnStartup);
            }
            catch
            {
                // Handle error
            }
        }

        private void SetRunOnStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string? exePath = Environment.ProcessPath;
                            if (exePath != null)
                            {
                                key.SetValue(AppName, $"\"{exePath}\"");
                            }
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch
            {
                // Handle permission errors etc.
            }
        }
    }
}
