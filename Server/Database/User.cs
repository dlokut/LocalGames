using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Server.Database
{
    public class User : IdentityUser
    {
        public bool ProfileCommentsEnabled { get; set; } = true;

        public string? ProfilePicFileName { get; set; } = null;

        public string? BackgroundPicFileName { get; set; } = null;

        public bool ProfileIsPrivate { get; set; } = false;

        public string IpAddress { get; set; }
    }
}
