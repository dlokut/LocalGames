using Microsoft.EntityFrameworkCore;
using Server.Database;

namespace Server.Managers;

public class GameManager
{
    private readonly IDbContextFactory<ServerDbContext> _dbContextFactory;

    private readonly IgdbManager _igdbManager;
    
    public const string GAMES_DIR = "Games";
    
    public const string SAVE_FILES_DIR = "SaveFiles";
    
    public GameManager(IDbContextFactory<ServerDbContext> dbContextFactory, IgdbManager igdbManager)
    {
        this._dbContextFactory = dbContextFactory;
        this._igdbManager = igdbManager;
    }

    public string GetGamesDir()
    {
        return GAMES_DIR;
    }

    public string GetSaveFileDir()
    {
        return SAVE_FILES_DIR;
    }

    public async Task<bool> UpdateGameMetadataAsync(Guid gameId, long igdbId)
    {
        using (ServerDbContext dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            Game gameToUpdate = await dbContext.Games.FindAsync(gameId);
            Game? gameWithMetadata = await ApplyGameMetadataAsync(gameToUpdate, igdbId);

            bool IGDB_ID_NOT_FOUND = gameWithMetadata == null;
            if (IGDB_ID_NOT_FOUND) return false;
            
            dbContext.Update(gameWithMetadata);

            await DeletePreviousArtworkAsync(gameId);
            List<Artwork>? newArtworks = await GetArtworksAsync(igdbId);
            if (newArtworks != null)
            {
                foreach (Artwork artwork in newArtworks)
                {
                    artwork.GameId = gameId;
                }
                
                await dbContext.Artworks.AddRangeAsync(newArtworks);
            }

            await dbContext.SaveChangesAsync();
            return true;
        }

    }

    private async Task DeletePreviousArtworkAsync(Guid gameId)
    {
        using (ServerDbContext dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            List<Artwork> artworks = dbContext.Artworks.Where(a => a.GameId == gameId).ToList();
            
            if (artworks.Count == 0) return;
            
            dbContext.Artworks.RemoveRange(artworks);
            await dbContext.SaveChangesAsync();
        }
    }
    
    public async Task ScanGameSavesDirectoryAsync(Guid gameId, string gameName, string userId)
    {
        List<GameSave> gameSaves = new List<GameSave>();

        string gameSavePath = SAVE_FILES_DIR + '/' + gameName;
        List<string> gameSavePaths = new List<string>();
        gameSavePaths = GetFilePathsInDirAndSubdirs(gameSavePaths, gameSavePath);

        foreach (string path in gameSavePaths)
        {
            gameSaves.Add(new GameSave()
            {
                GameId = gameId,
                UserId = userId,
                Directory = path
            });
        }

        using (ServerDbContext dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            foreach (GameSave gameSave in gameSaves)
            {
                GameSave? existingSave = await dbContext.GameSaves.FindAsync(userId, gameId,
                    gameSave.Directory);

                if (existingSave == null)
                {
                    await dbContext.GameSaves.AddAsync(gameSave);
                }
            }
            
            await dbContext.SaveChangesAsync();
        }
    }

    private List<string> GetFilePathsInDirAndSubdirs(List<string> filePaths, string dir)
    {
        foreach (string filePath in Directory.GetFiles(dir))
        {
            string filePathFromGameRoot = filePath.Substring(filePath.IndexOf('/') + 1);
            filePaths.Add(filePathFromGameRoot);
        }

        foreach (string subDir in Directory.GetDirectories(dir))
        {
            filePaths = GetFilePathsInDirAndSubdirs(filePaths, subDir);
        }

        return filePaths;
    }
    
    public async Task ScanGamesDirectoryAsync()
    {
        List<Game> foundGames = await GetGamesInDir(GAMES_DIR);

        if (foundGames.Count == 0) return;
        
        foreach (Game foundGame in foundGames)
        {
            string gamePath = GAMES_DIR + '/' + foundGame.Name;
            
            List<GameFile> gameFiles = GetGameFiles(gamePath);
            Game gameWithMetadata = await ApplyGameMetadataAsync(foundGame);
            gameWithMetadata.FileSize = GetTotalGameSize(gameFiles);
            
            List<Artwork>? artworks = await GetArtworksAsync(gameWithMetadata.IgdbId.Value);

            await AddGameToDbAsync(gameWithMetadata, gameFiles, artworks);
            
        }
    }

    // Using void instead of Task here as this is an event method
    // stateInfo is required for timer, but not actually used
    public async void ScanGamesDirectoryEvent(object? stateInfo)
    {
        await ScanGamesDirectoryAsync();
    }

    private async Task<List<Game>> GetGamesInDir(string dir)
    {
        List<Game> foundGames = new List<Game>();
        
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            string gameFolderName = subDir.Substring(subDir.IndexOf('/') + 1);

            if (await GameAlreadyAddedAsync(gameFolderName)) continue;
            
            foundGames.Add(new Game()
            {
                Name = gameFolderName,
                FolderName = gameFolderName
            });
        }

        return foundGames;
    }

    private async Task<bool> GameAlreadyAddedAsync(string gameFolderName)
    {
        using (ServerDbContext dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            Game? foundGame = dbContext.Games.Where(g => g.FolderName == gameFolderName)
                .ToList()
                .FirstOrDefault(defaultValue: null);

            return foundGame != null;
        }
    }

    private List<GameFile> GetGameFiles(string dir)
    {
        List<GameFile> foundGameFiles = new List<GameFile>();
        
        List<string> foundGameFilePaths = new List<string>();
        foundGameFilePaths = GetFilePathsInDirAndSubdirs(foundGameFilePaths, dir);
        foreach (string filePathFromGameRoot in foundGameFilePaths)
        {
            /*
             TODO: This gets the actual size (amount of data in file) rather than disk usage (amount of data + headers,
             block endings etc. Might need to get disk usage instead if transferring over network doesn't work later
             Taken from https://www.tutorialspoint.com/how-do-you-get-the-file-size-in-chash
             */
            
            // Required to add games dir for file info method
            string filePathFromGamesDir = GAMES_DIR + '/' + filePathFromGameRoot;
            long fileSizeBytes = new FileInfo(filePathFromGamesDir).Length;
            
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

    private async Task<Game?> ApplyGameMetadataAsync(Game game, long? gameIgdbId = null)
    {
        gameIgdbId ??= await _igdbManager.GetGameIdAsync(game.Name);

        if (!gameIgdbId.HasValue)
        {
            return null;
        }

        game.IgdbId = gameIgdbId;

        game.Name = await _igdbManager.GetGameNameAsync(gameIgdbId.Value);
        game.Summary = await _igdbManager.GetGameSummaryAsync(gameIgdbId.Value);
        game.CoverUrl = "https:" + await _igdbManager.GetCoverUrlAsync(gameIgdbId.Value);

        return game;
    }

    private async Task<List<Artwork>?> GetArtworksAsync(long igdbGameId)
    {
        List<string>? artworkUrls = await _igdbManager.GetArtworkUrlsAsync(igdbGameId);

        if (artworkUrls == null)
        {
            return null;
        }

        // TODO: Might not be very readable
        List<Artwork> artworks = artworkUrls.Select(url => new Artwork()
        {
            ArtworkUrl = "https:" + url
        }).ToList();

        return artworks;
    }

    private async Task AddGameToDbAsync(Game game, List<GameFile> gameFiles, List<Artwork>? artworks)
    {
        using (ServerDbContext dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            await dbContext.Games.AddAsync(game);

            if (artworks != null)
            {
                foreach (Artwork artwork in artworks)
                {
                    artwork.GameId = game.Id;
                }
                
                await dbContext.Artworks.AddRangeAsync(artworks);
            }

            foreach (GameFile gameFile in gameFiles)
            {
                gameFile.GameId = game.Id;
            }
                
            await dbContext.GameFiles.AddRangeAsync(gameFiles);

            await dbContext.SaveChangesAsync();
        }

    }
}