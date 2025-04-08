using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Database;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly ServerDbContext dbContext;

        private readonly UserManager<User> userManager;

        private readonly SignInManager<User> _signInManager;

        public UserController(UserManager<User> userManager, ServerDbContext dbContext, SignInManager<User> signInManager)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            _signInManager = signInManager;
        }

        [HttpPost]
        [Route("v1/PostRegister")]
        public async Task<ActionResult> Register([FromBody] string password, string username)
        {
            User newUser = new User();
            newUser.UserName = username;

            IdentityResult registerResult = await userManager.CreateAsync(newUser, password);

            if (registerResult.Succeeded) return Ok();
            else return BadRequest(registerResult.Errors);
        }

        [HttpPost]
        [Route("v1/PostLogin")]
        public async Task<ActionResult> Login([FromBody] string password, string username)
        {
            User user = await userManager.FindByNameAsync(username);
            if (user == null) return BadRequest("Username not found");

            Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, false);

            if (signInResult.Succeeded)
            {
                await _signInManager.PasswordSignInAsync(user, password, true, false);
                return Ok();
            }

            else return BadRequest("Incorrect password");
        }

        [HttpGet]
        [Route("v1/GetUsername")]
        public async Task<IActionResult> GetUsernameAsync(string userId)
        {
            User? foundUser = await dbContext.Users.FindAsync(userId);
            if (foundUser == null) return BadRequest("User with given id not found");

            return Ok(foundUser.UserName);
        }

        [HttpPost]
        [Route("v1/PostAddFriend")]
        public async Task<IActionResult> PostAddFriendAsync(string friendId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            User friendToAdd = await dbContext.Users.FindAsync(friendId);
            if (friendToAdd == null) return BadRequest("User with given id not found");

            if (currentUser.Id == friendToAdd.Id) return BadRequest("Cannot add self as friend");

            // Add check for friend already being added
            Friends newFriends = new Friends();
            newFriends.User1Id = currentUser.Id;
            newFriends.User2Id = friendToAdd.Id;

            await dbContext.Friends.AddAsync(newFriends);
            await dbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
