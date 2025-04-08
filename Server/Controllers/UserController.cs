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

        public UserController(UserManager<User> userManager, ServerDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        [HttpPost]
        [Route("v1/PostRegister")]
        public async Task<ActionResult> Register([FromBody] string password, string username)
        {
            User newUser = new User();
            // await userManager.CreateAsync()
            return Ok();
        }
    }
}
