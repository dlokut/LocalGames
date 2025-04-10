using Server.Database;

namespace Server.Services;

public class GameDiscoveryService : BackgroundService, IDisposable
{
    private const string GAMES_DIR = "Games";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<Game> games = GetGamesInDir(GAMES_DIR);

            foreach (Game game in games)
            {
                Console.WriteLine(game.Name);
            }
            
            await Task.Delay(1000);
        }
    }

    private void ScanGamesDirectory()
    {
        
    }

    List<Game> GetGamesInDir(string dir)
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
}