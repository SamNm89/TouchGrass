using System;
using System.ComponentModel;
using System.Windows;

namespace TouchGrass
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel(new Services.GameService(), new Services.LauncherService());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}