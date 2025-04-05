using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    public class BannedUsers
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        public User User { get; set; }

        public string IpAddress { get; set; }
    }
}
