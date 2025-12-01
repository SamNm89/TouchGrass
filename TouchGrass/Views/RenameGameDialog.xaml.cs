using System.Windows;

namespace TouchGrass.Views
{
    public partial class RenameGameDialog : Window
    {
        public string GameTitle { get; private set; } = string.Empty;

        public RenameGameDialog(string currentTitle)
        {
            InitializeComponent();
            NameTextBox.Text = currentTitle;
            NameTextBox.Focus();
            NameTextBox.SelectAll();

            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow(this)));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter a valid name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GameTitle = NameTextBox.Text.Trim();
            DialogResult = true;
        }
    }
}
