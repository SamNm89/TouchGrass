using System;
using System.Diagnostics;
using System.IO;
using TouchGrass.Models;

namespace TouchGrass.Services
{
    public class LauncherService
    {
        public bool LaunchGame(GameModel game, out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                if (game.IsSteamShortcut)
                {
                    return LaunchSteamGame(game, out errorMessage);
                }
                else
                {
                    return LaunchStandardGame(game, out errorMessage);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Unexpected error launching game: {ex.Message}";
                return false;
            }
        }

        private bool LaunchSteamGame(GameModel game, out string? errorMessage)
        {
            errorMessage = null;
            string url = game.ExecutablePath;

            // Basic check to ensure it starts with the protocol if it's just an ID
            // Assuming if it's just numbers, it's an AppID.
            if (long.TryParse(url, out _))
            {
                url = $"steam://rungameid/{url}";
            }
            else if (!url.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
            {
                // If it's not an ID and doesn't start with steam://, we might want to warn or try to prepend.
                // For now, let's assume the user might have pasted just "rungameid/123" or similar, 
                // but to be safe, if it doesn't look like a steam URL, we'll try to prepend.
                // However, the requirement said "parse the correct ID or protocol string".
                // Let's be safe: if it doesn't start with steam://, prepend it.
                url = $"steam://{url}";
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to launch Steam game: {ex.Message}";
                return false;
            }
        }

        private bool LaunchStandardGame(GameModel game, out string? errorMessage)
        {
            errorMessage = null;
            string path = game.ExecutablePath;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "Executable file not found.";
                return false;
            }

            try
            {
                string? workingDir = Path.GetDirectoryName(path);
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = workingDir,
                    UseShellExecute = true // Often helpful for games, though not strictly required if direct exe
                });
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to launch game executable: {ex.Message}";
                return false;
            }
        }
    }
}
