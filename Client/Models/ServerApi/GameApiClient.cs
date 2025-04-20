using System;
using System.Collections.Generic;
using System.IO;
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
    public async Task DownloadGameAsync(Guid gameId)
    {
        List<GameFile> gameFiles = await GetGameFileInfoAsync(gameId);

        foreach (GameFile gameFile in gameFiles)
        {
            await DownloadGameFileAsync(gameId, gameFile.Directory);
        }
        
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
    }

}

public record ServerGame(Guid id, long fileSize, string name, string summary, string coverUrl);