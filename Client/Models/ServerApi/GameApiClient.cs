using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Client.Database;

namespace Client.Models.ServerApi;

public class GameApiClient
{
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
    
    //public async Task<>
}

public record ServerGame(Guid id, long fileSize, string name, string summary, string coverUrl);