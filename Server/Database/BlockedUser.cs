using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    [PrimaryKey(nameof(BlockedId), nameof(BlockerId))]
    public class BlockedUser
    {
        [ForeignKey(nameof(Blocker))]
        public string BlockerId { get; set; }

        public User Blocker { get; set; }

        [ForeignKey(nameof(Blocked))]
        public string BlockedId { get; set; }

        public User Blocked { get; set; }



    }
}
