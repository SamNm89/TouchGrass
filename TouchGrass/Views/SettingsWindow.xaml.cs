using System.Windows;
using System.Windows.Input;
using TouchGrass.Services;

namespace TouchGrass.Views
{
    public partial class SettingsWindow : Window
    {
        private Key _selectedKey;
        private ModifierKeys _selectedModifiers;
        private readonly SettingsService _settingsService;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = _settingsService.CurrentSettings;
            _selectedKey = settings.HotkeyKey;
            _selectedModifiers = settings.HotkeyModifiers;
            RunOnStartupCheckBox.IsChecked = settings.RunOnStartup;
            ShowTrayIconCheckBox.IsChecked = settings.ShowTrayIcon;

            UpdateHotkeyText();
        }

        private void UpdateHotkeyText()
        {
            HotkeyTextBox.Text = $"{_selectedModifiers} + {_selectedKey}";
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ignore single modifier keys
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LWin || e.Key == Key.RWin || 
                e.Key == Key.System)
            {
                return;
            }

            e.Handled = true;

            _selectedModifiers = Keyboard.Modifiers;
            _selectedKey = (e.Key == Key.System) ? e.SystemKey : e.Key;

            UpdateHotkeyText();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var newSettings = new AppSettings
            {
                HotkeyKey = _selectedKey,
                HotkeyModifiers = _selectedModifiers,
                RunOnStartup = RunOnStartupCheckBox.IsChecked == true,
                ShowTrayIcon = ShowTrayIconCheckBox.IsChecked == true
            };

            _settingsService.SaveSettings(newSettings);
            DialogResult = true;
        }
    }
}
