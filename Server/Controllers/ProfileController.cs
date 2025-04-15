using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Server.Attributes;
using Server.Database;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfileController : Controller
{
    private const string PROFILE_PICS_DIR = "ProfilePics";

    private readonly string[] _ACCEPTED_BACKGROUND_PROFILE_PIC_EXTENSIONS =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".gif"
    ];
    
    private readonly ServerDbContext _dbContext;

    private readonly UserManager<User> _userManager;

    public ProfileController(ServerDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpPost]
    [Route("v1/PostProfilePic")]
    public async Task<IActionResult> PostProfilePicAsync(IFormFile profilePic)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return BadRequest("Must be signed in to upload playtimes");
        }

        string picExtension = Path.GetExtension(profilePic.FileName);
        if (!_ACCEPTED_BACKGROUND_PROFILE_PIC_EXTENSIONS.Contains(picExtension))
        {
            return BadRequest("File must be of type " + String.Join(", ", 
                _ACCEPTED_BACKGROUND_PROFILE_PIC_EXTENSIONS));
        }

        string profilePicFilePath = Path.Combine(PROFILE_PICS_DIR, profilePic.FileName);
        using FileStream fileStream = new FileStream(profilePicFilePath, FileMode.Create);
        await profilePic.CopyToAsync(fileStream);
        
        return Ok();
    }
    
    
}