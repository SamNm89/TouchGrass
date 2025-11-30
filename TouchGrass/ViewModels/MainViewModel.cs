using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TouchGrass.Models;
using TouchGrass.Services;

namespace TouchGrass.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly GameService _gameService;
        private readonly LauncherService _launcherService;
        private List<GameModel> _allGames = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<GameModel> Games { get; } = new();

        public MainViewModel(GameService gameService, LauncherService launcherService)
        {
            _gameService = gameService;
            _launcherService = launcherService;
            LoadGames();
        }

        private void LoadGames()
        {
            _allGames = _gameService.LoadGames();
            FilterGames();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterGames();
        }

        private void FilterGames()
        {
            Games.Clear();
            var query = SearchText?.Trim();

            IEnumerable<GameModel> filtered = string.IsNullOrWhiteSpace(query)
                ? _allGames
                : _allGames.Where(g => g.Title.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var game in filtered)
            {
                Games.Add(game);
            }
        }

        [RelayCommand]
        private void AddGame()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Games (*.exe;*.url;*.lnk)|*.exe;*.url;*.lnk|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filename in openFileDialog.FileNames)
                {
                    string defaultTitle = System.IO.Path.GetFileNameWithoutExtension(filename);
                    
                    // Extract Icon
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string coversDir = System.IO.Path.Combine(appData, "GhostLauncher", "Covers");
                    string? extractedIconPath = Helpers.IconHelper.ExtractIconToPng(filename, coversDir);

                    // Show Dialog for details (passing the extracted icon path)
                    var dialog = new Views.AddGameDialog(defaultTitle, extractedIconPath ?? string.Empty);
                    if (dialog.ShowDialog() == true)
                    {
                        string title = dialog.GameTitle;
                        // Use the extracted icon path (or empty if failed)
                        string finalCoverPath = extractedIconPath ?? string.Empty;

                        var newGame = new GameModel
                        {
                            Title = title,
                            ExecutablePath = filename,
                            CoverImagePath = finalCoverPath,
                            IsSteamShortcut = filename.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
                        };

                        _gameService.AddGame(newGame);
                        _allGames.Add(newGame);
                    }
                }
                FilterGames();
            }
        }

        [RelayCommand]
        private void LaunchGame(GameModel? game)
        {
            if (game == null) return;
            if (_launcherService.LaunchGame(game, out string? errorMessage))
            {
                // Success
            }
            else
            {
                System.Windows.MessageBox.Show(errorMessage ?? "Unknown error launching game.");
            }
        }

        [RelayCommand]
        private void RemoveGame(GameModel? game)
        {
            if (game == null) return;

            _gameService.RemoveGame(game.Id);
            _allGames.RemoveAll(g => g.Id == game.Id);
            FilterGames();
        }

        [RelayCommand]
        private void RenameGame(GameModel? game)
        {
            if (game == null) return;

            var dialog = new Views.RenameGameDialog(game.Title);
            if (dialog.ShowDialog() == true)
            {
                game.Title = dialog.GameTitle;
                _gameService.UpdateGame(game);
                FilterGames();
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow(App.SettingsService);
            if (settingsWindow.ShowDialog() == true)
            {
                var app = System.Windows.Application.Current as App;
                app?.RegisterHotkey();
                app?.UpdateTrayIconVisibility();
            }
        }
    }
}
