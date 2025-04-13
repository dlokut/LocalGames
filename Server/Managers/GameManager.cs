using Microsoft.EntityFrameworkCore;
using Server.Database;

namespace Server.Managers;

public class GameManager
{
    private readonly IDbContextFactory<ServerDbContext> dbContextFactory;

    private readonly IgdbManager igdbManager;
    
    public const string GAMES_DIR = "Games";
    
    public GameManager(IDbContextFactory<ServerDbContext> dbContextFactory, IgdbManager igdbManager)
    {
        this.dbContextFactory = dbContextFactory;
        this.igdbManager = igdbManager;
    }

    public string GetGamesDir()
    {
        return GAMES_DIR;
    }

    public async Task<bool> UpdateGameMetadataAsync(Guid gameId, long igdbId)
    {
        using (ServerDbContext dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            Game gameToUpdate = await dbContext.Games.FindAsync(gameId);
            Game? gameWithMetadata = await ApplyGameMetadataAsync(gameToUpdate, igdbId);

            bool IGDB_ID_NOT_FOUND = gameWithMetadata == null;
            if (IGDB_ID_NOT_FOUND) return false;
            
            dbContext.Update(gameWithMetadata);

            await DeletePreviousArtworkAsync(gameId);
            List<Artwork>? newArtworks = await GetArtworksAsync(igdbId);
            if (newArtworks != null)
            {
                foreach (Artwork artwork in newArtworks)
                {
                    artwork.GameId = gameId;
                }
                
                await dbContext.Artworks.AddRangeAsync(newArtworks);
            }

            await dbContext.SaveChangesAsync();
            return true;
        }

    }

    private async Task DeletePreviousArtworkAsync(Guid gameId)
    {
        using (ServerDbContext dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            List<Artwork> artworks = dbContext.Artworks.Where(a => a.GameId == gameId).ToList();
            
            if (artworks.Count == 0) return;
            
            dbContext.Artworks.RemoveRange(artworks);
            await dbContext.SaveChangesAsync();
        }
    }
    
    public async Task ScanGamesDirectoryAsync()
    {
        List<Game> foundGames = await GetGamesInDir(GAMES_DIR);

        if (foundGames.Count == 0) return;
        
        foreach (Game foundGame in foundGames)
        {
            string gamePath = GAMES_DIR + '/' + foundGame.Name;
            
            List<GameFile> gameFiles = GetGameFiles(gamePath);
            Game gameWithMetadata = await ApplyGameMetadataAsync(foundGame);
            gameWithMetadata.FileSize = GetTotalGameSize(gameFiles);
            
            List<Artwork>? artworks = await GetArtworksAsync(gameWithMetadata.IgdbId.Value);

            await AddGameToDbAsync(gameWithMetadata, gameFiles, artworks);
            
        }
    }

    // Using void instead of Task here as this is an event method
    // stateInfo is required for timer, but not actually used
    public async void ScanGamesDirectoryEvent(object? stateInfo)
    {
        await ScanGamesDirectoryAsync();
    }

    private async Task<List<Game>> GetGamesInDir(string dir)
    {
        List<Game> foundGames = new List<Game>();
        
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            string gameName = subDir.Substring(subDir.IndexOf('/') + 1);

            if (await GameAlreadyAddedAsync(gameName)) continue;
            
            foundGames.Add(new Game()
            {
                Name = gameName
            });
        }

        return foundGames;
    }

    private async Task<bool> GameAlreadyAddedAsync(string gameName)
    {
        using (ServerDbContext dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            Game? foundGame = dbContext.Games.Where(g => g.Name == gameName)
                .ToList()
                .FirstOrDefault(defaultValue: null);

            return foundGame != null;
        }
    }

    private List<GameFile> GetGameFiles(string dir)
    {
        List<GameFile> foundGameFiles = new List<GameFile>();

        foreach (string fileInDir in Directory.GetFiles(dir))
        {
            /*
             TODO: This gets the actual size (amount of data in file) rather than disk usage (amount of data + headers,
             block endings etc. Might need to get disk usage instead if transferring over network doesn't work later
             Taken from https://www.tutorialspoint.com/how-do-you-get-the-file-size-in-chash
             */
            long fileSizeBytes = new FileInfo(fileInDir).Length;
            string filePathFromGameRoot = fileInDir.Substring(fileInDir.IndexOf('/') + 1);
            
            foundGameFiles.Add(new GameFile()
            {
                Directory = filePathFromGameRoot,
                FileSizeBytes = fileSizeBytes
            });
        }

        return foundGameFiles;
    }

    private long GetTotalGameSize(List<GameFile> gameFiles)
    {
        return gameFiles.Select(gf => gf.FileSizeBytes).Sum();
    }

    private async Task<Game?> ApplyGameMetadataAsync(Game game, long? gameIgdbId = null)
    {
        gameIgdbId ??= await igdbManager.GetGameIdAsync(game.Name);

        if (!gameIgdbId.HasValue)
        {
            return null;
        }

        game.IgdbId = gameIgdbId;

        game.Summary = await igdbManager.GetGameSummaryAsync(gameIgdbId.Value);
        game.CoverUrl = "https:" + await igdbManager.GetCoverUrlAsync(gameIgdbId.Value);

        return game;
    }

    private async Task<List<Artwork>?> GetArtworksAsync(long igdbGameId)
    {
        List<string>? artworkUrls = await igdbManager.GetArtworkUrlsAsync(igdbGameId);

        if (artworkUrls == null)
        {
            return null;
        }

        // TODO: Might not be very readable
        List<Artwork> artworks = artworkUrls.Select(url => new Artwork()
        {
            ArtworkUrl = "https:" + url
        }).ToList();

        return artworks;
    }

    private async Task AddGameToDbAsync(Game game, List<GameFile> gameFiles, List<Artwork>? artworks)
    {
        using (ServerDbContext dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            await dbContext.Games.AddAsync(game);

            if (artworks != null)
            {
                foreach (Artwork artwork in artworks)
                {
                    artwork.GameId = game.Id;
                }
                
                await dbContext.Artworks.AddRangeAsync(artworks);
            }

            foreach (GameFile gameFile in gameFiles)
            {
                gameFile.GameId = game.Id;
            }
                
            await dbContext.GameFiles.AddRangeAsync(gameFiles);

            await dbContext.SaveChangesAsync();
        }

    }
}