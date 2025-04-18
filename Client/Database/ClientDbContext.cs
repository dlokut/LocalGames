using Microsoft.EntityFrameworkCore;

namespace Client.Database;

public class ClientDbContext : DbContext
{
    public ClientDbContext()  
    {
       Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=ClientDb.db");
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<DownloadedGame> DownloadedGames { get; set; }
    
    public DbSet<ProtonSettings> ProtonSettings { get; set; }

    public DbSet<ProtonEnvVariable> ProtonEnvVariables { get; set; }

    public DbSet<GameSave> GameSaves { get; set; }
    
    public DbSet<Playtime> Playtimes { get; set; }
    
    public DbSet<Artwork> Artworks { get; set; }
    
}