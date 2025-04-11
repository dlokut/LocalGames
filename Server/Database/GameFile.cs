using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    [PrimaryKey(nameof(GameId), nameof(Directory))]
    public class GameFile
    {
        [ForeignKey(nameof(Game))]
        public Guid GameId { get; set; }

        public Game Game { get; set; }

        public string Directory { get; set; }

        public long FileSizeBytes { get; set; }
    }
}
