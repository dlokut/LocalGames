using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    [PrimaryKey(nameof(UserId), nameof(GameId))]
    public class ProfileGame
    {
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        public User User { get; set; }

        [ForeignKey(nameof(Game))]
        public Guid GameId { get; set; }

        public Game Game { get; set; } 
    }
}
