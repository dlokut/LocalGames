using System.ComponentModel.DataAnnotations;

namespace Server.Database
{
    public class Game
    {
        [Key]
        public Guid Id { get; set; }

        public long IgdbId { get; set; }

        public int FileSize { get; set; }

        public string Name { get; set; }

        public string Summary { get; set; }

        public string CoverUrl { get; set; }
    }
}
