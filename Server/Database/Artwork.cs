using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    [PrimaryKey(nameof(GameId), nameof(ArtworkUrl))]
    public class Artwork
    {
        [ForeignKey(nameof(Game))]
        public Guid GameId { get; set; }

        public Game Game { get; set; }

        public string ArtworkUrl { get; set; }
    }
}
