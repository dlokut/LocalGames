using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    [PrimaryKey(nameof(User1Id), nameof(User2Id))]
    public class Friends
    {
        [ForeignKey("User1")]
        public string User1Id { get; set; }

        public User User1 { get; set; }

        [ForeignKey("User2")]
        public string User2Id { get; set; }

        public User User2 { get; set; }
    }
}
