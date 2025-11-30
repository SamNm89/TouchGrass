using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TouchGrass.Models;

namespace TouchGrass.Services
{
    public class GameService
    {
        private readonly string _dataFilePath;
        private List<GameModel> _games;

        public GameService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appData, "GhostLauncher");
            _dataFilePath = Path.Combine(folderPath, "games.json");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _games = LoadGames();
        }

        public List<GameModel> LoadGames()
        {
            if (!File.Exists(_dataFilePath))
            {
                return new List<GameModel>();
            }

            try
            {
                string json = File.ReadAllText(_dataFilePath);
                return JsonSerializer.Deserialize<List<GameModel>>(json) ?? new List<GameModel>();
            }
            catch
            {
                // In case of corruption or error, return empty list for now
                return new List<GameModel>();
            }
        }

        private void SaveGames()
        {
            try
            {
                string json = JsonSerializer.Serialize(_games, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception)
            {
                // Handle save errors (log or rethrow depending on requirements)
            }
        }

        public List<GameModel> GetAllGames()
        {
            return _games;
        }

        public void AddGame(GameModel game)
        {
            _games.Add(game);
            SaveGames();
        }

        public void RemoveGame(Guid id)
        {
            var game = _games.FirstOrDefault(g => g.Id == id);
            if (game != null)
            {
                _games.Remove(game);
                SaveGames();
            }
        }

        public void UpdateGame(GameModel updatedGame)
        {
            var index = _games.FindIndex(g => g.Id == updatedGame.Id);
            if (index != -1)
            {
                _games[index] = updatedGame;
                SaveGames();
            }
        }
    }
}
