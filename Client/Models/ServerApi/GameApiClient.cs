using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.AccessControl;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Client.Database;
using System.Threading;
using Microsoft.EntityFrameworkCore;

namespace Client.Models.ServerApi;

public class GameApiClient
{
    private const string GAMES_DIR = "Games";
    
    private const string ALL_GAMES_ENDPOINT = "Game/v1/GetAllGames";

    private readonly ProtonManager _protonManager;
    
    private readonly SaveFileManager _saveFileManager;

    public GameApiClient()
    {
        _protonManager = new ProtonManager();
        _saveFileManager = new SaveFileManager();

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

    public async Task<ProtonSettings> GetProtonSettingsAsync(Guid gameId)
    {
        await using (ClientDbContext dbContext = new ClientDbContext())
        {
            return await dbContext.ProtonSettings.FindAsync(gameId);
        }
    }

    public async Task<List<DownloadedGame>> GetAllDownloadedGames()
    {
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            return await dbContext.DownloadedGames.ToListAsync();
        }
    }

    public async Task<Dictionary<string, string>> GetExeGameFilesAsync(Guid gameId)
    {
        await using (ClientDbContext dbContext = new ClientDbContext())
        {
            List<GameFile> gameExeFiles = dbContext.GameFiles.Where(gf =>
                (gf.GameId == gameId) && (gf.Directory.Contains("exe"))).ToList();

            Dictionary<string, string> gameExeFilesByFileName = gameExeFiles.ToDictionary(
                gef => Path.GetFileName(gef.Directory),
                gef => gef.Directory
            );

            return gameExeFilesByFileName;
        }
    }

    public async Task<string?> GetMainExeFileName(Guid gameId)
    {
        await using (ClientDbContext dbContext = new ClientDbContext())
        {
            GameFile? foundGameFile = await dbContext.GameFiles.FirstOrDefaultAsync(
                gf => (gf.GameId == gameId) && (gf.IsMainExecutable));

            if (foundGameFile == null) return null;
            else return Path.GetFileName(foundGameFile.Directory);

        }
    }

    private const string UPLOAD_METADATA_ENDPOINT = "Game/v1/PostGameMetadata";

    public async Task UploadMetadataAsync(ServerGame serverGame)
    {
         ServerInfoManager serverInfoManager = new ServerInfoManager();
         HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();

         string endpoint = UPLOAD_METADATA_ENDPOINT +
                           $"?gameId={serverGame.Id}&name={serverGame.Name}&summary={serverGame.Summary}&coverImageUrl={serverGame.CoverUrl}";
         await clientWithCookies.PostAsync(endpoint, null);
    }

    private const string UPLOAD_GAME_ENDPOINT = "Game/v1/PostUploadGame";

    public async Task UploadGameAsync(string gameName, List<string> gameFilesDirs)
    {
         ServerInfoManager serverInfoManager = new ServerInfoManager();
         HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();
         clientWithCookies.Timeout = Timeout.InfiniteTimeSpan;

         MultipartFormDataContent content = GetMultipartFormDataContent(gameFilesDirs);

         string endpoint = UPLOAD_GAME_ENDPOINT + $"?gameName={gameName}";
         HttpResponseMessage response = await clientWithCookies.PostAsync(endpoint, content);
    }

    private const string UPLOAD_GAME_SAVES_ENDPOINT = "Game/v1/PostUploadSaveFiles";
    public async Task UploadGameSavesAsync(Guid gameId)
    {
        ProtonSettings settings;
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            settings = await dbContext.ProtonSettings.FindAsync(gameId);
        }
        List<string> gameSaves = _saveFileManager.FindSaveFiles(settings.PrefixDir);

        MultipartFormDataContent content = GetMultipartFormDataContent(gameSaves);
        
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();
        clientWithCookies.Timeout = Timeout.InfiniteTimeSpan;
        
        string endpoint = UPLOAD_GAME_SAVES_ENDPOINT + $"?gameId={gameId}";
        HttpResponseMessage response = await clientWithCookies.PostAsync(endpoint, content);
    }

    private MultipartFormDataContent GetMultipartFormDataContent(List<string> gameFilesDirs)
    {
        List<string> filePaths = new List<string>();
        const string ALL_FILES = "*";
        foreach (string filesDirs in gameFilesDirs)
        {
            // Whether string is a directory
            if (Directory.Exists(filesDirs))
            {
                filePaths.AddRange(Directory.GetFiles(filesDirs, ALL_FILES,
                    SearchOption.AllDirectories));
            }
            
            else filePaths.Add(filesDirs);
        }


        // First adds num of files, then file dirs as string content, then the actual files (see upload game method in 
        // server code)
        MultipartFormDataContent content = new MultipartFormDataContent();
        
        int fileCount = filePaths.Count;
        content.Add(new StringContent(fileCount.ToString()), "itemCount");


        foreach (string filePath in filePaths)
        {
            content.Add(new StringContent(filePath), filePath);
        }
        
        foreach (string filePath in filePaths)
        {
            string fileName = Path.GetFileName(filePath);
            content.Add(new StreamContent(new FileStream(filePath, FileMode.Open)), filePath, fileName);
        }

        return content;

    }
        
    public async Task DownloadGameAsync(ServerGame game)
    {
        List<GameFile> gameFiles = await GetGameFileInfoAsync(game.Id);

        foreach (GameFile gameFile in gameFiles)
        {
            await DownloadGameFileAsync(game.Id, gameFile.Directory);
        }

        await AddGameToDbAsync(game, gameFiles);

        await AfterGameDownloaded(game.Id);
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

    private async Task AfterGameDownloaded(Guid gameId)
    {
        await DownloadGameSaves(gameId);
    }
    
    private async Task DownloadGameSaves(Guid gameId)
    {
        ProtonSettings protonSettings;
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            protonSettings = await dbContext.ProtonSettings.FindAsync(gameId);
        }

        List<string> saveFileDirs = await GetSaveFileInfoAsync(gameId);

        foreach (string saveFileDir in saveFileDirs)
        {
            await DownloadGameSave(gameId, saveFileDir);
        }

    }

    private const string SAVE_FILE_DOWNLOAD_ENDPOINT = "Game/v1/GetSaveFile";
    private async Task DownloadGameSave(Guid gameId, string saveDir)
    {
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();

        string endpoint = SAVE_FILE_DOWNLOAD_ENDPOINT + "?gameId=" + gameId + "&saveFileDir=" + saveDir;
        using Stream response = await clientWithCookies.GetStreamAsync(endpoint);

        // Need to remove game name at start of path
        string savePath = saveDir.Substring(saveDir.IndexOf('/'));
        string savePathDir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(savePathDir))
        {
            Directory.CreateDirectory(savePathDir);
        }
        
        FileStream gameFileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

        await response.CopyToAsync(gameFileStream);
        await gameFileStream.FlushAsync();
        gameFileStream.Close();
    }

    private const string SAVE_FILE_INFO_ENDPOINT = "Game/v1/GetSaveFilesInfo";
    private async Task<List<string>> GetSaveFileInfoAsync(Guid gameId)
    {
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();
        
        string endpoint = SAVE_FILE_INFO_ENDPOINT + "?gameId=" + gameId;
        using HttpResponseMessage response = await clientWithCookies.GetAsync(endpoint);
         
        string responseContentJson = await response.Content.ReadAsStringAsync();
                 
        JsonSerializerOptions jsonOptions = serverInfoManager.sharedJsonOptions;
        var gameSaves = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(responseContentJson, jsonOptions);

        List<string> gameSaveDirs = gameSaves.Select(gs => gs["directory"]).ToList();
                 
        return gameSaveDirs;
               
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

    private const string UPLOAD_PLAYTIME_ENDPOINT = "Game/v1/PostPlaytime";
    public async Task UploadPlaytimeAsync(Guid gameId, int playtimeMins)
    {
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();

        string endpoint = UPLOAD_PLAYTIME_ENDPOINT + $"?gameId={gameId}&playtimeMins={playtimeMins}";
        await clientWithCookies.PostAsync(endpoint, null);

    }

    private const string GET_PLAYTIME_ENDPOINT = "Game/v1/GetPlaytime";

    public async Task<int> GetPlaytimeAsync(Guid gameId)
    {
         ServerInfoManager serverInfoManager = new ServerInfoManager();
         HttpClient clientWithCookies = await serverInfoManager.GetClientWithLoginCookieAsync();
 
         string endpoint = GET_PLAYTIME_ENDPOINT + $"?gameId={gameId}";
         HttpResponseMessage response = await clientWithCookies.GetAsync(endpoint);

         JsonSerializerOptions jsonOptions = serverInfoManager.sharedJsonOptions;
         string responseJson = await response.Content.ReadAsStringAsync();
         int playtimeMins = JsonSerializer.Deserialize<int>(responseJson, jsonOptions);

         return playtimeMins;
    }

}

public record ServerGame(Guid Id, long FileSize, string Name, string Summary, string CoverUrl);