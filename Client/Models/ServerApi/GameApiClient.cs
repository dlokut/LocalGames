using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Client.Database;

namespace Client.Models.ServerApi;

public class GameApiClient
{
    private const string GAMES_DIR = "Games";
    
    private const string ALL_GAMES_ENDPOINT = "Game/v1/GetAllGames";

    private readonly ProtonManager _protonManager;

    public GameApiClient()
    {
        _protonManager = new ProtonManager();
    }
    
    public async Task<List<ServerGame>> GetAllGamesOnServer()
    {
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();
        
        using HttpResponseMessage response = await clientWithCookies.GetAsync(ALL_GAMES_ENDPOINT);

        string responseContentJson = await response.Content.ReadAsStringAsync();
        
        JsonSerializerOptions jsonOptions = serverInfoManager.sharedJsonOptions;
        List<ServerGame> allGamesOnServer = JsonSerializer.Deserialize<List<ServerGame>>(responseContentJson,
            jsonOptions);
        
        return allGamesOnServer;

    }
    public async Task DownloadGameAsync(ServerGame game)
    {
        List<GameFile> gameFiles = await GetGameFileInfoAsync(game.Id);

        foreach (GameFile gameFile in gameFiles)
        {
            await DownloadGameFileAsync(game.Id, gameFile.Directory);
        }

        await AddGameToDbAsync(game, gameFiles);

    }
    
    private const string GAME_FILE_INFO_ENDPOINT = "Game/v1/GetGameFilesInfo";
    private async Task<List<GameFile>> GetGameFileInfoAsync(Guid gameId)
    {
         ServerInfoManager serverInfoManager = new ServerInfoManager();
         HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();

         string endpoint = GAME_FILE_INFO_ENDPOINT + "?gameId=" + gameId;
         using HttpResponseMessage response = await clientWithCookies.GetAsync(endpoint);
 
         string responseContentJson = await response.Content.ReadAsStringAsync();
         
         JsonSerializerOptions jsonOptions = serverInfoManager.sharedJsonOptions;
         List<GameFile> gameFiles = JsonSerializer.Deserialize<List<GameFile>>(responseContentJson,
             jsonOptions);
         
         return gameFiles;
       
    }

    private const string GAME_FILE_DOWNLOAD_ENDPOINT = "Game/v1/GetDownloadGameFile";
    private async Task DownloadGameFileAsync(Guid gameId, string fileDir)
    {
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();

        string endpoint = GAME_FILE_DOWNLOAD_ENDPOINT + "?gameId=" + gameId + "&fileDir=" + fileDir;
        using Stream response = await clientWithCookies.GetStreamAsync(endpoint);

        string fileFolder = Path.Combine(Directory.GetCurrentDirectory(), GAMES_DIR, Path.GetDirectoryName(fileDir));
        if (!Directory.Exists(fileFolder))
        {
            Directory.CreateDirectory(fileFolder);
        }
        
        string absoluteFileDir = Path.Combine(Directory.GetCurrentDirectory(), GAMES_DIR, fileDir);
        FileStream gameFileStream = new FileStream(absoluteFileDir, FileMode.Create, FileAccess.Write, FileShare.None);

        await response.CopyToAsync(gameFileStream);
        await gameFileStream.FlushAsync();
        gameFileStream.Close();
    }

    private async Task AddGameToDbAsync(ServerGame game, List<GameFile> gameFiles)
    {
        DownloadedGame downloadedGame = ServerGameToDownloadedGame(game);

        foreach (GameFile gameFile in gameFiles)
        {
            gameFile.GameId = downloadedGame.Id;
        }

        ProtonSettings defaultSettings = _protonManager.CreateDefaultProtonSettings(downloadedGame);

        using (ClientDbContext dbContext = new ClientDbContext())
        {
            await dbContext.DownloadedGames.AddAsync(downloadedGame);
            await dbContext.GameFiles.AddRangeAsync(gameFiles);
            await dbContext.ProtonSettings.AddAsync(defaultSettings);

            await dbContext.SaveChangesAsync();
        }
    }

    private DownloadedGame ServerGameToDownloadedGame(ServerGame serverGame)
    {
        return new DownloadedGame()
        {
            Id = serverGame.Id,
            Name = serverGame.Name,
            Summary = serverGame.Summary,
            CoverUrl = serverGame.CoverUrl,
            FileSize = serverGame.FileSize
        };
    }

    public async Task UninstallGameAsync(Guid gameId)
    {
        List<GameFile> gameFiles;
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            gameFiles = dbContext.GameFiles.Where(gf => gf.GameId == gameId).ToList();
        }

        foreach (GameFile gameFile in gameFiles)
        {
            File.Delete(Path.Combine(GAMES_DIR, gameFile.Directory));
        }

        await RemoveGameFromDb(gameId);

    }

    private async Task RemoveGameFromDb(Guid gameId)
    {
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            DownloadedGame gameToRemove = await dbContext.DownloadedGames.FindAsync(gameId);

            dbContext.DownloadedGames.Remove(gameToRemove);
            await dbContext.SaveChangesAsync();
        }
    }

}

public record ServerGame(Guid Id, long FileSize, string Name, string Summary, string CoverUrl);