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
    }
}
