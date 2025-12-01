using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TouchGrass.Views
{
    public partial class AddGameDialog : Window
    {
        public string GameTitle { get; private set; } = string.Empty;
        public string CoverImagePath { get; private set; } = string.Empty;

        public AddGameDialog(string defaultTitle, string coverImagePath)
        {
            InitializeComponent();
            TitleBox.Text = defaultTitle;
            GameTitle = defaultTitle;
            CoverImagePath = coverImagePath;

            if (!string.IsNullOrEmpty(coverImagePath))
            {
                try
                {
                    CoverImagePreview.Source = new BitmapImage(new Uri(coverImagePath));
                }
                catch
                {
                    // Handle invalid image
                }
            }

            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow(this)));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            GameTitle = TitleBox.Text;
            DialogResult = true;
            Close();
        }
    }
}
