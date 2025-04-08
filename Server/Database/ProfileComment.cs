using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    public class ProfileComment
    {
        [Key]
        public string tId { get; set; }

        [ForeignKey(nameof(Commenter))]
        public string CommenterId { get; set; }

        public User Commenter { get; set; }

        [ForeignKey(nameof(UserProfile))]
        public string UserProfileId { get; set; }

        public User UserProfile { get; set; }

        public string Content { get; set; }
    }
}
