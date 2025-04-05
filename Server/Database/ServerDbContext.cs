using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.Database
{
    public class ServerDbContext : IdentityDbContext<User>
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Friends> Friends { get; set; }

        public DbSet<BlockedUser> BlockedUsers { get; set; }

        public DbSet<BannedUsers> BannedUsers { get; set; }

        public DbSet<ProfileComment> Comments { get; set; }

        public DbSet<Artwork> Artworks { get; set; }

        public DbSet<Game> Games { get; set; }

        public DbSet<ProfileGames> ProfileGames { get; set; }

        public DbSet<GameSaves> GameSaves { get; set; }

        public DbSet<Playtime> Playtimes { get; set; }


    }
}
