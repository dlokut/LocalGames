using Server.Database;

namespace Server.Services;

public class GameDiscoveryService : BackgroundService, IDisposable
{
    private const string GAMES_DIR = "Games";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<GameFile> games = GetGameFiles(GAMES_DIR + "/TestGame");

            foreach (GameFile game in games)
            {
                Console.WriteLine(game.Directory);
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

    List<GameFile> GetGameFiles(string dir)
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
}