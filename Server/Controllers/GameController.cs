using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Server.Attributes;
using Server.Database;
using Server.Managers;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    
    public class GameController : Controller
    {
        private readonly ServerDbContext _dbContext;

        private readonly UserManager<User> _userManager;

        private readonly GameManager _gameManager;

        private const long NO_SIZE_LIMIT = long.MaxValue;

        public GameController(ServerDbContext dbContext, GameManager gameManager, UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _gameManager = gameManager;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("v1/GetAllGames")]
        public async Task<List<Game>> GetAllGamesAsync()
        {
            return _dbContext.Games.ToList();
        }

        [HttpGet]
        [Route("v1/GetGameFilesInfo")]
        public async Task<IActionResult> GetGameFilesInfoAsync(Guid gameId)
        {
            if (!await GameIdInDbAsync(gameId))
            {
                return BadRequest("Unknown game id");
            }
            
            // Removing gameid and game properties as this seems to cause issue with the framework json serialiser
            List<GameFile> gameFiles = _dbContext.GameFiles.Where(gf => gf.GameId == gameId).ToList();
            foreach (GameFile gameFile in gameFiles)
            {
                gameFile.GameId = Guid.Empty;
                gameFile.Game = null;
            }
            return Ok(gameFiles);
        }

        [HttpPost]
        [Route("v1/PostDeleteGame")]
        public async Task<IActionResult> PostDeleteGameAsync(Guid gameId)
        {
            Game? gameToDelete = await _dbContext.Games.FindAsync(gameId);

            if (gameToDelete == null)
            {
                return BadRequest("Unknown game id");
            }
            
            DeleteGameFiles(_gameManager.GetGamesDir() + '/' + gameToDelete.Name);

            _dbContext.Games.Remove(gameToDelete);

            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        private void DeleteGameFiles(string gameDir)
        {
            const string ALL_FILES_DIRECTORIES = "*";
            foreach (string filePath in Directory.GetFiles(gameDir, ALL_FILES_DIRECTORIES,
                         SearchOption.AllDirectories))
            {
                System.IO.File.Delete(filePath);
            }

            foreach (string directory in Directory.GetDirectories(gameDir, ALL_FILES_DIRECTORIES,
                         SearchOption.AllDirectories))
            {
                Directory.Delete(directory);
            }
            
            Directory.Delete(gameDir);
        }

        [HttpPost]
        [Route("v1/PostIgdbId")]
        public async Task<IActionResult> PostIgdbIdAsync(Guid gameId, long newIgdbId)
        {
            if (!await GameIdInDbAsync(gameId))
            {
                return BadRequest("Unknown game id");
            }

            bool updateSuccessful = await _gameManager.UpdateGameMetadataAsync(gameId, newIgdbId);

            if (updateSuccessful) return Ok();
            else return BadRequest("Given igdb id not found");
        }

        [HttpPost]
        [Route("v1/PostPlaytime")]
        public async Task<IActionResult> PostPlaytimeAsync(Guid gameId, int playtimeMins)
        {
            User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
            {
                return BadRequest("Must be signed in to upload playtimes");
            }
            
            if (!await GameIdInDbAsync(gameId))
            {
                return BadRequest("Unknown game id");
            }

            Playtime? foundPlaytime = await _dbContext.Playtimes.FindAsync(currentUser.Id, gameId);

            if (foundPlaytime == null)
            {
                Playtime newPlaytime = new Playtime()
                {
                    UserId = currentUser.Id,
                    GameId = gameId,
                    PlaytimeMins = playtimeMins
                };

                await _dbContext.Playtimes.AddAsync(newPlaytime);
            }

            else
            {
                foundPlaytime.PlaytimeMins = playtimeMins;
                _dbContext.Playtimes.Update(foundPlaytime);
            }

            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Route("v1/GetPlaytime")]
        public async Task<IActionResult> GetPlaytimeAsync(Guid gameId)
        {
            User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
            {
                return BadRequest("Must be signed in to get playtime");
            }
            
            if (!await GameIdInDbAsync(gameId))
            {
                return BadRequest("Unknown game id");
            }

            Playtime? foundPlaytime = await _dbContext.Playtimes.FindAsync(currentUser.Id, gameId);
            if (foundPlaytime == null)
            {
                const int NO_PLAYTIME_RECORDED = 0;
                return Ok(NO_PLAYTIME_RECORDED);
            }

            else return Ok(foundPlaytime.PlaytimeMins);
        }

        [HttpGet]
        [Route("v1/GetGameMetadata")]
        public async Task<IActionResult> GetGameMetadataAsync(Guid gameId)
        {
            Game? foundGame = await _dbContext.Games.FindAsync(gameId);

            if (foundGame == null)
            {
                return BadRequest("Game id not found");
            }

            // Removing references to other objects to avoid issues with json serializer
            foundGame.GameFiles = null;
            foundGame.Artworks = null;
            foundGame.Saves = null;
            foundGame.GameFiles = null;

            return Ok(foundGame);
        }

        [HttpPost]
        [Route("v1/PostGameMetadata")]
        public async Task<IActionResult> PostGameMetadataAsync(Guid gameId, string? summary, string? coverImageUrl)
        {
            Game? foundGame = await _dbContext.Games.FindAsync(gameId);
            if (foundGame == null)
            {
                return BadRequest("Game id not found");
            }

            if (summary != null) foundGame.Summary = summary;
            if (coverImageUrl != null) foundGame.CoverUrl = coverImageUrl;

            _dbContext.Games.Update(foundGame);
            await _dbContext.SaveChangesAsync();
            
            return Ok();
        }
        

        private async Task<bool> GameIdInDbAsync(Guid gameId)
        {
            Game? foundGame = await _dbContext.Games.FindAsync(gameId);

            return foundGame != null;
        }
        
        # region Download/Upload
        
        //TODO: Game files and saves have mime type when downloaded, but unsure if this is added by browser.
        //Might need to change if not working on the client app

        /*
         * Game data is uploaded as multipart form request. The first form item must have the number of files sent,
         * followed by the directories of the files as text values, then followed by the files themselves. Files and
         * directories must be keyed by the file name. The item count form item key doesn't matter.
         */
        
        [HttpPost]
        [Route("v1/PostUploadGame")]
        // Disabled so files don't get loaded into memory/disk
        [DisableFormValueModelBinding]
        [RequestSizeLimit(NO_SIZE_LIMIT)]
        public async Task<IActionResult> PostUploadGameAsync([FromQuery] string gameName)
        {
            if (!IsMultipartFormData(HttpContext.Request))
            {
                return BadRequest("Must be of content type multipart/form-data");
            }
            
            string gamePath = _gameManager.GetGamesDir() + "/" + gameName;
            await UploadMultipartFilesAsync(Request.Body, Request.ContentType, gamePath);

            await _gameManager.ScanGamesDirectoryAsync();
            
            return Ok();
        }

        [HttpGet]
        [Route("v1/GetDownloadGameFile")]
        public async Task<IActionResult> GetDownloadGameFileAsync(Guid gameId, string fileDir)
        {
            GameFile? foundGameFile = await _dbContext.GameFiles.FindAsync(gameId, fileDir);

            if (foundGameFile == null)
            {
                return BadRequest("Game file not found");
            }

            string gameFilePathFromGamesDir = _gameManager.GetGamesDir() + '/' + foundGameFile.Directory;

            return Ok(new FileStream(gameFilePathFromGamesDir, FileMode.Open, FileAccess.Read, FileShare.None,
                1024));
        }

        /*
         * Game saves are uploaded as multipart form request. The first form item must have the number of files sent,
         * followed by the directories of the files as text values, then followed by the files themselves. Files and
         * directories must be keyed by the file name. The item count form item key doesn't matter.
         */
        
        [HttpPost]
        [Route("v1/PostUploadSaveFiles")]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(NO_SIZE_LIMIT)]
        public async Task<IActionResult> PostUploadSaveFilesAsync([FromQuery] Guid gameId)
        {
            if (!IsMultipartFormData(HttpContext.Request))
            {
                return BadRequest("Must be of content type multipart/form-data");
            }

            User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
            {
                return BadRequest("Must be signed in to upload game files");
            }
            

            Game? foundGame = await _dbContext.Games.FindAsync(gameId);
            if (foundGame == null)
            {
                return BadRequest("Given game id not found");
            }

            string saveFilesPath = _gameManager.GetSaveFileDir() + '/' + foundGame.Name;

            await UploadMultipartFilesAsync(Request.Body, Request.ContentType, saveFilesPath);

            await _gameManager.ScanGameSavesDirectoryAsync(gameId, foundGame.Name, currentUser.Id);

            return Ok();

        }

        [HttpGet]
        [Route("v1/GetSaveFile")]
        public async Task<IActionResult> GetSaveFileAsync(Guid gameId, string saveFileDir)
        {
            User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
            {
                return BadRequest("Must be signed in to upload game files");
            }
            
            GameSave? foundGameSave = await _dbContext.GameSaves.FindAsync(currentUser.Id, gameId,
                saveFileDir);
            
            if (foundGameSave == null)
            {
                return BadRequest("Game file not found");
            }
            
            string gameFilePathFromGamesDir = _gameManager.GetSaveFileDir() + '/' + foundGameSave.Directory;

            return Ok(new FileStream(gameFilePathFromGamesDir, FileMode.Open, FileAccess.Read, FileShare.None,
                1024));
        }
        
        private bool IsMultipartFormData(HttpRequest request)
        {
            if (!request.HasFormContentType) return false;

            if (!request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }


        private async Task UploadMultipartFilesAsync(Stream requestBodyStream, string contentTypeHeader,
            string rootPath)
        {
            string boundary = GetMultipartBoundary(MediaTypeHeaderValue.Parse(contentTypeHeader));

            Directory.CreateDirectory(rootPath);
            
            MultipartReader multipartReader = new MultipartReader(boundary, requestBodyStream);
            Dictionary<string, string>? fileDirs = await GetMultipartFileDirsAsync(multipartReader);
            
            MultipartSection? section = await multipartReader.ReadNextSectionAsync();
            while (section != null)
            {
                FileMultipartSection? fileSection = section.AsFileSection();
                if (fileSection == null) continue;
                
                CreateDirectoriesForFile(fileDirs[fileSection.Name], rootPath);

                string filePath = rootPath + "/" + fileDirs[fileSection.Name];
                using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                           1024))
                {
                    await fileSection.FileStream.CopyToAsync(stream);
                }

                section = await multipartReader.ReadNextSectionAsync();
            }
        }

        private string GetMultipartBoundary(MediaTypeHeaderValue header)
        {
            string boundary = HeaderUtilities.RemoveQuotes(header.Boundary).Value;

            return boundary;
        }

        // Will return null if there was en error reading sections
        private async Task<Dictionary<string, string>?> GetMultipartFileDirsAsync(MultipartReader multipartReader)
        {
            MultipartSection? section = await multipartReader.ReadNextSectionAsync();
            FormMultipartSection? formMultipartSection = section.AsFormDataSection();

            if (formMultipartSection == null)
            {
                return null;
            }
            
            int itemCount = int.Parse(await formMultipartSection.GetValueAsync());

            var gameFileDirs = new Dictionary<string, string>();

            for (int i = 0; i < itemCount; i++)
            {
                section = await multipartReader.ReadNextSectionAsync();
                formMultipartSection = section.AsFormDataSection();
                
                string sectionKey = formMultipartSection.Name;
                string sectionValue = await formMultipartSection.GetValueAsync();

                gameFileDirs[sectionKey] = sectionValue;
            }
            
            return gameFileDirs;
        }

        private void CreateDirectoriesForFile(string gameFilePath, string rootPath)
        {
            string previousDirs = "";
            while (gameFilePath.Contains('/'))
            {
                string dirName = gameFilePath.Substring(0, gameFilePath.IndexOf('/'));
                Directory.CreateDirectory(rootPath + '/' + previousDirs + dirName);

                previousDirs += dirName + "/";
                
                gameFilePath = gameFilePath.Substring(gameFilePath.IndexOf('/') + 1);
            }

        }
        
        # endregion
        
    }
}
