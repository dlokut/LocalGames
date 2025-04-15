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
    
    private const string BACKGROUND_PICS_DIR = "BackgroundPics";

    private readonly string[] _ACCEPTED_BACKGROUND_PROFILE_PIC_EXTENSIONS =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".gif"
    ];

    private const string MISSING_PROFILE_BACKGROUND_PIC_FILE = "missing-image.png";
    
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
            return BadRequest("Must be signed in to upload profile pic");
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

        currentUser.ProfilePicFileName = profilePic.FileName;
        _dbContext.Users.Update(currentUser);
        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }

    [HttpPost]
    [Route("v1/PostBackgroundPic")]
    public async Task<IActionResult> PostBackgroundPicAsync(IFormFile backgroundPic)
    {
         User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
         if (currentUser == null)
         {
             return BadRequest("Must be signed in to upload background pic");
         }
 
         string picExtension = Path.GetExtension(backgroundPic.FileName);
         if (!_ACCEPTED_BACKGROUND_PROFILE_PIC_EXTENSIONS.Contains(picExtension))
         {
             return BadRequest("File must be of type " + String.Join(", ", 
                 _ACCEPTED_BACKGROUND_PROFILE_PIC_EXTENSIONS));
         }
 
         string backgroundPicFilePath = Path.Combine(BACKGROUND_PICS_DIR, backgroundPic.FileName);
         using FileStream fileStream = new FileStream(backgroundPicFilePath, FileMode.Create);
         await backgroundPic.CopyToAsync(fileStream);
 
         currentUser.BackgroundPicFileName = backgroundPic.FileName;
         _dbContext.Users.Update(currentUser);
         await _dbContext.SaveChangesAsync();
         
         return Ok();       
    }

    [HttpGet]
    [Route("v1/GetProfilePic")]
    public async Task<IActionResult> GetProfilePicAsync(string userId)
    {
        User? foundUser = await _dbContext.Users.FindAsync(userId);

        if (foundUser == null)
        {
            return BadRequest("User id not found");
        }

        string profilePicFileName = foundUser.ProfilePicFileName;
        profilePicFileName ??= MISSING_PROFILE_BACKGROUND_PIC_FILE;
        
        string profilePicFilePath = Path.Combine(PROFILE_PICS_DIR, profilePicFileName);

        return Ok(new FileStream(profilePicFilePath, FileMode.Open, FileAccess.Read));
    }
    
    [HttpGet]
    [Route("v1/GetBackgroundPic")]
    public async Task<IActionResult> GetBackgroundPicAsync(string userId)
    {
        User? foundUser = await _dbContext.Users.FindAsync(userId);

        if (foundUser == null)
        {
            return BadRequest("User id not found");
        }

        string backgroundPicFileName = foundUser.BackgroundPicFileName;
        backgroundPicFileName ??= MISSING_PROFILE_BACKGROUND_PIC_FILE;
        
        string backgroundPicFilePath = Path.Combine(BACKGROUND_PICS_DIR, backgroundPicFileName);
        
        return Ok(new FileStream(backgroundPicFilePath, FileMode.Open, FileAccess.Read));
    }

    [HttpPost]
    [Route("v1/PostComment")]
    public async Task<IActionResult> PostCommentAsync(string profileId, string commentContent)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return BadRequest("Must be signed in to upload background pic");
        }
         
        User? profile = await _dbContext.Users.FindAsync(profileId);
        if (profile == null)
        {
            return BadRequest("User id not found");
        }

        if (profile.ProfileIsPrivate || profile.ProfileCommentsEnabled == false)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        ProfileComment newComment = new ProfileComment()
        {
            UserProfileId = profile.Id,
            CommenterId = currentUser.Id,
            Content = commentContent
        };

        await _dbContext.Comments.AddAsync(newComment);
        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }

    [HttpGet]
    [Route("v1/GetProfileComments")]
    public async Task<IActionResult> GetProfileCommentsAsync(string profileId)
    {
        User? profile = await _dbContext.Users.FindAsync(profileId);
        if (profile == null)
        {
            return BadRequest("User id not found");
        }
        
        List<ProfileComment> profileComments = _dbContext.Comments.Where(c => c.UserProfileId == profileId)
            .ToList();

        return Ok(profileComments);
    }

    [HttpGet]
    [Route("v1/GetProfileCommentsEnabled")]
    public async Task<IActionResult> GetProfileCommentsEnabled(string profileId)
    {
        User? profile = await _dbContext.Users.FindAsync(profileId);
        if (profile == null)
        {
            return BadRequest("User id not found");
        }
        
        return Ok(profile.ProfileCommentsEnabled);
    }

    [HttpPost]
    [Route("v1/PostProfileCommentsEnabled")]
    public async Task<IActionResult> PostProfileCommentsEnabledAsync(bool profileCommentsEnabled)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return BadRequest("Must be signed in to set profile comments enabled/disabled");
        }

        currentUser.ProfileCommentsEnabled = profileCommentsEnabled;
        _dbContext.Users.Update(currentUser);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpGet]
    [Route("v1/GetProfileIsPrivate")]
    public async Task<IActionResult> GetProfileIsPrivateAsync(string profileId)
    {
        User? profile = await _dbContext.Users.FindAsync(profileId);
        if (profile == null)
        {
            return BadRequest("User id not found");
        }

        return Ok(profile.ProfileIsPrivate);
    }

    [HttpGet]
    [Route("v1/PostProfileIsPrivate")]
    public async Task<IActionResult> PostProfileIsPrivateAsync(bool profileIsPrivate)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return BadRequest("Must be signed in to set profile visibility");
        }
        
        currentUser.ProfileIsPrivate = profileIsPrivate;
        _dbContext.Users.Update(currentUser);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    [Route("v1/PostProfileGame")]
    public async Task<IActionResult> PostProfileGameAsync(Guid gameId)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return BadRequest("Must be signed in to upload profile games");
        }

        Game? foundGame = await _dbContext.Games.FindAsync(gameId);
        if (foundGame == null)
        {
            return BadRequest("Game id not found");
        }

        ProfileGame? foundProfileGame = await _dbContext.ProfileGames.FindAsync(currentUser.Id, gameId);
        if (foundProfileGame != null)
        {
            return BadRequest("Profile game already added");
        }

        ProfileGame profileGame = new ProfileGame()
        {
            UserId = currentUser.Id,
            GameId = foundGame.Id
        };

        await _dbContext.ProfileGames.AddAsync(profileGame);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpGet]
    [Route("v1/GetProfileGames")]
    public async Task<IActionResult> GetProfileGamesAsync(string profileId)
    {
        User? profile = await _dbContext.Users.FindAsync(profileId);
        if (profile == null)
        {
            return BadRequest("User id not found");
        }

        List<ProfileGame> profileGames = _dbContext.ProfileGames.Where(pg => pg.UserId == profileId).ToList();

        // Removing references to other objects to avoid issues with json serializer
        foreach (ProfileGame profileGame in profileGames)
        {
            profileGame.GameId = Guid.Empty;
            profileGame.Game = null;
            profileGame.UserId = null;
            profileGame.User = null;
        }

        return Ok(profileGames);
    }

    [HttpDelete]
    [Route("v1/DeleteProfileGame")]
    public async Task<IActionResult> DeleteProfileGameAsync(Guid gameId)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return BadRequest("Must be signed in to delete profile games");
        }

        ProfileGame? profileGame = await _dbContext.ProfileGames.FindAsync(currentUser.Id, gameId);
        if (profileGame == null)
        {
            return BadRequest("Profile game not found");
        }

        _dbContext.ProfileGames.Remove(profileGame);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
    
    
    
    
    
    
    
    
    
    
    
}