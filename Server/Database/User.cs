using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Server.Database
{
    public class User : IdentityUser
    {
        public bool ProfileCommentsEnabled { get; set; }

        public string ProfilePicFileName { get; set; }

        public string BackgroundPicFileName { get; set; }

        public bool ProfileIsPrivate { get; set; }
    }
}
