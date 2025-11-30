using System;
using System.Collections.Generic;

namespace TouchGrass.Models
{
    public class GameModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public bool IsSteamShortcut { get; set; }

        // Future Proofing
        public List<string>? Tags { get; set; }
        public long? InstallSizeInBytes { get; set; }
    }
}
