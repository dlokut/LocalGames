﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.Database
{
    public class ServerDbContext : IdentityDbContext<User>
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> dbContextOptions) : base(dbContextOptions)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Friends> Friends { get; set; }

        public DbSet<BlockedUser> BlockedUsers { get; set; }

        public DbSet<BannedUser> BannedUsers { get; set; }

        public DbSet<ProfileComment> Comments { get; set; }

        public DbSet<Artwork> Artworks { get; set; }

        public DbSet<Game> Games { get; set; }

        public DbSet<ProfileGame> ProfileGames { get; set; }

        public DbSet<GameSave> GameSaves { get; set; }

        public DbSet<Playtime> Playtimes { get; set; }

        public DbSet<GameFile> GameFiles { get; set; }


    }
}
