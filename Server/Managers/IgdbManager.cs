using IGDB;
using IGDB.Models;
using IgdbGame = IGDB.Models.Game;
using IgdbArtwork = IGDB.Models.Artwork;

namespace Server.Managers
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

        public async Task<string> GetGameSummaryAsync(long gameIgdbId)
        {
            IgdbGame[] foundGames = await igdbClient.QueryAsync<IgdbGame>(IGDBClient.Endpoints.Games, 
                query: $"where id = {gameIgdbId}; fields summary;");

            IgdbGame foundGame = foundGames.First();

            return foundGame.Summary;
        }

        public async Task<string?> GetCoverUrlAsync(long gameIgdbId)
        {
            string? coverId = await GetCoverIdAsync(gameIgdbId);

            if (coverId == null)
            {
                return null;
            }

            string coverUrl = IGDB.ImageHelper.GetImageUrl(coverId, ImageSize.CoverBig);

            return coverUrl;
        }

        private async Task<string?> GetCoverIdAsync(long gameIgdbId)
        {
            Cover[] foundCovers = await igdbClient.QueryAsync<Cover>(IGDBClient.Endpoints.Covers, 
                query: $"where game = {gameIgdbId}; fields image_id;");

            if (foundCovers.Length == 0)
            {
                return null;
            }

            Cover foundCover = foundCovers.First();

            return foundCover.ImageId;
        }

        public async Task<List<string>?> GetArtworkUrlsAsync(long gameIgdbId)
        {
            List<string>? artworkIds = await GetArtworkIdsAsync(gameIgdbId);

            if (artworkIds == null)
            {
                return null;
            }

            List<string> artworkUrls = new List<string>();

            foreach (string id in artworkIds)
            {
                artworkUrls.Add(IGDB.ImageHelper.GetImageUrl(id, ImageSize.HD720));
            }

            return artworkUrls;
        }
        
        private async Task<List<string>?> GetArtworkIdsAsync(long gameIgdbId)
        {
            IgdbArtwork[] foundArtworks = await igdbClient.QueryAsync<IgdbArtwork>(IGDBClient.Endpoints.Artworks, 
                query: $"where game = {gameIgdbId}; fields image_id;");

            if (foundArtworks.Length == 0)
            {
                return null;
            }

            return foundArtworks.Select(a => a.ImageId).ToList();
        }

        
    }
}
