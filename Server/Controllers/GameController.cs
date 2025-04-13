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

        private readonly GameManager _gameManager;

        private const long NO_SIZE_LIMIT = long.MaxValue;

        public GameController(ServerDbContext dbContext, GameManager gameManager)
        {
            _dbContext = dbContext;
            _gameManager = gameManager;
        }

        [HttpGet]
        [Route("v1/GetAllGames")]
        public async Task<List<Game>> GetAllGamesAsync()
        {
            return _dbContext.Games.ToList();
        }

        [HttpGet]
        [Route("v1/GetGameFiles")]
        public async Task<IActionResult> GetGameFilesAsync(Guid gameId)
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
        

        private async Task<bool> GameIdInDbAsync(Guid gameId)
        {
            Game? foundGame = await _dbContext.Games.FindAsync(gameId);

            return foundGame != null;
        }
        
        # region Download/Upload

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
            
            await UploadMultipartGameFilesAsync(Request.Body, Request.ContentType, gameName);

            await _gameManager.ScanGamesDirectoryAsync();
            
            return Ok();
        }

        private bool IsMultipartFormData(HttpRequest request)
        {
            if (!request.HasFormContentType) return false;

            if (!request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        // These are disabled to not load large files into memory

        private async Task UploadMultipartGameFilesAsync(Stream requestBodyStream, string contentTypeHeader,
            string gameName)
        {
            string boundary = GetMultipartBoundary(MediaTypeHeaderValue.Parse(contentTypeHeader));

            string gamePath = _gameManager.GetGamesDir() + "/" + gameName;
            Directory.CreateDirectory(gamePath);
            
            MultipartReader multipartReader = new MultipartReader(boundary, requestBodyStream);
            MultipartSection? section = await multipartReader.ReadNextSectionAsync();
                
            while (section != null)
            {
                FileMultipartSection? fileSection = section.AsFileSection();

                if (fileSection == null) continue;

                
                string filePath = gamePath + "/" + fileSection.FileName;

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
        
        # endregion
        
    }
}
