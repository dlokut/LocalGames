using Server.Database;

namespace Server.Services;

public class GameDiscoveryService : BackgroundService
{

    private readonly IServiceProvider serviceProvider;
    
    private IgdbManager igdbManager;

    private Timer? serviceTimer = null;
    
    private const string GAMES_DIR = "Games";

    private const int TIMER_INTERVAL_MS = 100 * 1000;

    private const int TIMER_START_DELAY_MS = 0;
    
    public GameDiscoveryService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string igdbClientId = Environment.GetEnvironmentVariable("IGDB_CLIENT_ID");
        string igdbClientSecret = Environment.GetEnvironmentVariable("IGDB_SECRET_ID");

        igdbManager = new IgdbManager(igdbClientId, igdbClientSecret);

        serviceTimer = new Timer(ScanGamesDirectory, null,
            TIMER_START_DELAY_MS, TIMER_INTERVAL_MS);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await serviceTimer.DisposeAsync();
        
        await base.StopAsync(cancellationToken);
    }

    private async void ScanGamesDirectory(Object? stateInfo)
    {
        List<Game> foundGames = GetGamesInDir(GAMES_DIR);

        if (foundGames.Count == 0) return;
        
        foreach (Game foundGame in foundGames)
        {
            string gamePath = GAMES_DIR + '/' + foundGame.Name;
            
            List<GameFile> gameFiles = GetGameFiles(gamePath);
            Game gameWithMetadata = await ApplyGameMetadata(foundGame);
            gameWithMetadata.FileSize = GetTotalGameSize(gameFiles);
            
            List<Artwork>? artworks = await GetArtworksAsync(gameWithMetadata.IgdbId.Value);

            await AddGameToDbAsync(gameWithMetadata, gameFiles, artworks);
            
        }
        
        Console.WriteLine("Finished");

    }

    private List<Game> GetGamesInDir(string dir)
    {
        List<Game> foundGames = new List<Game>();
        
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            string gameName = subDir.Substring(subDir.IndexOf('/') + 1);

            if (GameAlreadyAdded(gameName)) continue;
            
            foundGames.Add(new Game()
            {
                Name = gameName
            });
        }

        return foundGames;
    }

    private bool GameAlreadyAdded(string gameName)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ServerDbContext dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

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

    private async Task<Game?> ApplyGameMetadata(Game game)
    {
        long? gameIgdbId = await igdbManager.GetGameIdAsync(game.Name);

        if (!gameIgdbId.HasValue)
        {
            return null;
        }

        game.IgdbId = gameIgdbId;

        game.Summary = await igdbManager.GetGameSummaryAsync(gameIgdbId.Value);
        game.CoverUrl = await igdbManager.GetCoverUrlAsync(gameIgdbId.Value);

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
            ArtworkUrl = url
        }).ToList();

        return artworks;
    }

    private async Task AddGameToDbAsync(Game game, List<GameFile> gameFiles, List<Artwork> artworks)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ServerDbContext dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            await dbContext.Games.AddAsync(game);
            
            foreach (Artwork artwork in artworks)
            {
                artwork.GameId = game.Id;
            }

            await dbContext.Artworks.AddRangeAsync(artworks);

            foreach (GameFile gameFile in gameFiles)
            {
                gameFile.GameId = game.Id;
            }

            await dbContext.GameFiles.AddRangeAsync(gameFiles);

            await dbContext.SaveChangesAsync();

        }
    }
}