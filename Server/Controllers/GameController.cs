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
