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
            
            _gameService.GamesChanged += OnGamesChanged;
            
            LoadGames();
        }

        private void OnGamesChanged()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LoadGames();
            });
        }

        private void LoadGames()
        {
            _allGames = _gameService.GetAllGames();
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
                    string coversDir = System.IO.Path.Combine(appData, "TouchGrass", "Covers");
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
                    }
                }
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
            }
        }

        [RelayCommand]
        private void ChangeIcon(GameModel? game)
        {
            if (game == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe;*.url;*.lnk)|*.exe;*.url;*.lnk|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filename = openFileDialog.FileName;
                
                // Extract Icon
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string coversDir = System.IO.Path.Combine(appData, "TouchGrass", "Covers");
                string? extractedIconPath = Helpers.IconHelper.ExtractIconToPng(filename, coversDir);

                if (!string.IsNullOrEmpty(extractedIconPath))
                {
                    game.CoverImagePath = extractedIconPath;
                    _gameService.UpdateGame(game);
                    
                    // Force UI update if necessary (ObservableObject should handle it if binding is correct)
                    // If the collection doesn't refresh automatically, we might need to trigger it.
                    // For now, assuming PropertyChanged on GameModel or CollectionChanged handles it.
                    // If GameModel implements INotifyPropertyChanged, it should work.
                    // If not, we might need to replace the item in the collection.
                    
                    // Let's check if we need to refresh the list to show the new icon immediately
                    // A simple way is to remove and re-add or just notify.
                    // But let's rely on binding first.
                    
                    // To be safe, let's refresh the list view if needed, but ideally GameModel notifies.
                    // If GameModel is not an ObservableObject, we might need to reload.
                    // Let's reload games to be sure for now, or just let it be.
                    // Actually, let's just call LoadGames() to refresh the UI state completely if needed,
                    // but that might be heavy. 
                    // Let's try to just update the object.
                    
                    // If the UI doesn't update, we can force a refresh.
                    // _allGames is a List, Games is ObservableCollection.
                    // Updating 'game' instance should update UI if 'game' raises PropertyChanged.
                    // If 'GameModel' is a simple class, it won't.
                    // Let's assume GameModel is simple for now and just refresh the list item.
                    
                    int index = Games.IndexOf(game);
                    if (index != -1)
                    {
                        Games[index] = game; // This triggers CollectionChanged Replace
                    }
                }
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow(App.SettingsService, _gameService);
            if (settingsWindow.ShowDialog() == true)
            {
                var app = System.Windows.Application.Current as App;
                app?.RegisterHotkey();
                app?.UpdateTrayIconVisibility();
            }
        }
    }
}
