﻿using System.ComponentModel.DataAnnotations;

namespace Server.Database
{
    public class Game
    {
        [Key]
        public Guid Id { get; set; }

        public long? IgdbId { get; set; }

        public long FileSize { get; set; }

        public string Name { get; set; }

        public string? Summary { get; set; }

        public string? CoverUrl { get; set; }
        
        public string FolderName { get; set; }

        public List<Playtime> Playtimes { get; set; }

        public List<GameSave> Saves { get; set; }

        public List<Artwork> Artworks { get; set; }
        
        public List<GameFile> GameFiles { get; set; }
    }
}
