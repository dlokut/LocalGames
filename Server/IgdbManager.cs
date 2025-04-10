using IGDB;
using System.Threading.Tasks;
using IgdbGame = IGDB.Models.Game;

namespace Server
{
    public class IgdbManager
    {
        private readonly IGDBClient igdbClient;

        public IgdbManager(string clientId, string clientSecret)
        {
            igdbClient = new IGDBClient(
                clientId,
                clientSecret
            );
        }

        public async Task<long?> GetGameIdAsync(string gameName)
        {
            IgdbGame[] foundGames = await igdbClient.QueryAsync<IgdbGame>(IGDBClient.Endpoints.Games, 
                query: $"search \"{gameName}\"; fields id;");

            if (foundGames.Length == 0)
            {
                return null;
            }

            IgdbGame foundGame = foundGames.First();

            return foundGame.Id.Value;

        }

        
    }
}
