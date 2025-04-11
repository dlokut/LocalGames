using Server.Database;

namespace Server.Services;

public class GameDiscoveryService : BackgroundService
{
    private IgdbManager igdbManager;

    private Timer? serviceTimer = null;
    
    private const string GAMES_DIR = "Games";

    private const int TIMER_INTERVAL_MS = 1 * 1000;

    private const int TIMER_START_DELAY_MS = 0;
    
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

        foreach (Game foundGame in foundGames)
        {
            string gamePath = GAMES_DIR + '/' + foundGame.Name;
            
            List<GameFile> gameFiles = GetGameFiles(gamePath);
            Game gameWithMetadata = await ApplyGameMetadata(foundGame);
            
            Console.WriteLine("Hit");
            
        }

    }

    private List<Game> GetGamesInDir(string dir)
    {
        List<Game> foundGames = new List<Game>();
        
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            string gameName = subDir.Substring(subDir.IndexOf('/') + 1);
            foundGames.Add(new Game()
            {
                Name = gameName
            });
        }

        return foundGames;
    }

    private List<GameFile> GetGameFiles(string dir)
    {
        List<GameFile> foundGameFiles = new List<GameFile>();

        foreach (string fileInDir in Directory.GetFiles(dir))
        {
            string filePathFromGameRoot = fileInDir.Substring(fileInDir.IndexOf('/') + 1);
            
            foundGameFiles.Add(new GameFile()
            {
                Directory = filePathFromGameRoot
            });
        }

        return foundGameFiles;
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
}