using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Database;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {
        private const string FORWARDED_IP_HEADER = "X-Forwarded-For";

        private readonly ServerDbContext dbContext;

        private readonly UserManager<User> userManager;

        private readonly SignInManager<User> _signInManager;


        public UserController(UserManager<User> userManager, ServerDbContext dbContext, SignInManager<User> signInManager)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            _signInManager = signInManager;
        }

        # region login/register

        [HttpPost]
        [Route("v1/PostRegister")]
        public async Task<ActionResult> Register([FromBody] string password, string username)
        {
            User newUser = new User();
            newUser.UserName = username;

            // First, checks if X-Forwarded-For header has an ip address, if not, get ip directly.
            // This is incase use is connecting behind a proxy (might also happen if server is set up behind a reverse proxy)
            string userIpAddress = HttpContext.Request.Headers[FORWARDED_IP_HEADER];
            userIpAddress ??= HttpContext.Connection.RemoteIpAddress.ToString();

            newUser.IpAddress = userIpAddress;

            IdentityResult registerResult = await userManager.CreateAsync(newUser, password);

            bool FIRST_USER = dbContext.Users.Count() == 1;
            if (FIRST_USER)
            {
                await userManager.AddToRoleAsync(newUser, "Admin");
            }

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

        # endregion

        [HttpGet]
        [Route("v1/GetUsername")]
        public async Task<IActionResult> GetUsernameAsync(string userId)
        {
            User? foundUser = await dbContext.Users.FindAsync(userId);
            if (foundUser == null) return BadRequest("User with given id not found");

            return Ok(foundUser.UserName);
        }

        [HttpGet]
        [Route("v1/GetAllUserIds")]
        public async Task<IActionResult> GetAllUserIds()
        {
            List<User> allUsers = await dbContext.Users.ToListAsync();
            List<string> userIds = allUsers.Select(user => user.Id).ToList();

            string test = HttpContext.Request.Headers[FORWARDED_IP_HEADER];
            string test2 = HttpContext.Connection.RemoteIpAddress.ToString();

            return Ok(userIds);
        }

        #region friends
        [HttpPost]
        [Route("v1/PostAddFriend")]
        public async Task<IActionResult> PostAddFriendAsync(string friendId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            User friendToAdd = await dbContext.Users.FindAsync(friendId);
            if (friendToAdd == null) return BadRequest("User with given id not found");

            if (currentUser.Id == friendToAdd.Id) return BadRequest("Cannot add self as friend");

            if (await FriendAlreadyAddedAsync(currentUser.Id, friendToAdd.Id))
            {
                return BadRequest("Friend already added");
            }

            Friends newFriends = new Friends();
            newFriends.User1Id = currentUser.Id;
            newFriends.User2Id = friendToAdd.Id;

            await dbContext.Friends.AddAsync(newFriends);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Route("v1/GetFriendIds")]
        public async Task<IActionResult> GetFriendIdsAsync()
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            IEnumerable<string>? friendIds = GetFriendIds(currentUser.Id);
            if (friendIds == null) return Ok("User has no friends");

            return Ok(friendIds);
        }

        [HttpPost]
        [Route("v1/PostRemoveFriend")]
        public async Task<IActionResult> PostRemoveFriend(string friendId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            if (currentUser.Id == friendId) return BadRequest("Cannot remove self as friend");

            Friends friends = await dbContext.Friends.FindAsync(currentUser.Id, friendId);
            friends ??= await dbContext.Friends.FindAsync(friendId, currentUser.Id);

            if (friends == null) return BadRequest("User with given id is not a friend");

            dbContext.Friends.Remove(friends);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        private async Task<bool> FriendAlreadyAddedAsync(string user1Id, string user2Id)
        {
            Friends foundFriends = await dbContext.Friends.FindAsync(user1Id, user2Id);
            foundFriends ??= await dbContext.Friends.FindAsync(user2Id, user1Id);

            if (foundFriends != null) return true;
            return false;
        }

        // TODO: get a better name to differentiate between this and http method
        private IEnumerable<string>? GetFriendIds(string userId)
        {
            List<string> friendIds = new List<string>();

            var foundFriends = dbContext.Friends.Where(f => (f.User1Id == userId) || (f.User2Id == userId));

            if (!foundFriends.Any()) return null;

            foreach (Friends friends in foundFriends)
            {
                // Figures out whether given user id is in friend 1 or 2, then uses the other friend for the friend id
                string friendId;
                if (friends.User1Id == userId) friendId = friends.User2Id;
                else friendId = friends.User1Id;

                friendIds.Add(friendId);
            }

            return friendIds;
        }

        #endregion

        #region blocking

        [HttpPost]
        [Route("v1/PostBlock")]
        public async Task<IActionResult> PostBlock(string blockedId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            User userToBlock = await dbContext.Users.FindAsync(blockedId);
            if (userToBlock == null) return BadRequest("User with given id not found");

            if (currentUser.Id == userToBlock.Id) return BadRequest("Cannot block self");

            if (await UserAlreadyBlockedAsync(currentUser.Id, userToBlock.Id))
            {
                return BadRequest("User already blocked");
            }

            BlockedUser blocked = new BlockedUser()
            {
                BlockerId = currentUser.Id,
                BlockedId = userToBlock.Id
            };

            await dbContext.BlockedUsers.AddAsync(blocked);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Route("v1/PostUnblock")]
        public async Task<IActionResult> PostUnblockAsync(string blockedId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            if (currentUser.Id == blockedId) return BadRequest("Cannot unblock self");

            BlockedUser blocked = await dbContext.BlockedUsers.FindAsync(blockedId, currentUser.Id);

            if (blocked == null) return BadRequest("User with given id is not blocked");

            dbContext.BlockedUsers.Remove(blocked);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Route("v1/GetUserBlocked")]
        public async Task<IActionResult> GetUserBlocked(string blockedId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            BlockedUser blocked = await dbContext.BlockedUsers.FindAsync(blockedId, currentUser.Id);

            return Ok(blocked != null);
        }

        [HttpGet]
        [Route("v1/GetIsBlockedBy")]
        public async Task<IActionResult> GetIsBlockedBy(string blockerId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            BlockedUser blocked = await dbContext.BlockedUsers.FindAsync(currentUser.Id, blockerId);

            return Ok(blocked != null);
        }


        private async Task<bool> UserAlreadyBlockedAsync(string blockerId, string blockedId)
        {
            BlockedUser blocked = await dbContext.BlockedUsers.FindAsync(blockedId, blockerId);
            return blocked != null;
        }

        #endregion

        #region banning

        [HttpPost]
        [Route("v1/PostBanUser")]
        public async Task<IActionResult> PostBanUserAsync(string userId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            if ((await userManager.IsInRoleAsync(currentUser, "Admin")) == false)
            {
                return Forbid();
            }

            if (currentUser.Id == userId) return BadRequest("Cannot ban self");

            User userToBan = await dbContext.Users.FindAsync(userId);
            if (userToBan == null) return BadRequest("User with given id not found");

            if ((await userManager.IsInRoleAsync(userToBan, "Admin"))) 
            {
                return Forbid();
            }

            BannedUser bannedUser = new BannedUser()
            {
                UserId = userToBan.Id,
                IpAddress = userToBan.IpAddress
            };

            await dbContext.BannedUsers.AddAsync(bannedUser);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Route("v1/PostUnbanUser")]
        public async Task<IActionResult> PostUnbanUser(string userId)
        {
            User currentUser = await userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return BadRequest("Must be logged in");

            if ((await userManager.IsInRoleAsync(currentUser, "Admin")) == false)
            {
                return Forbid();
            }

            if (currentUser.Id == userId) return BadRequest("Cannot unban self");

            BannedUser bannedUser = dbContext.BannedUsers.Find(userId);
            if (bannedUser == null) return BadRequest("User with given id already not banned");

            dbContext.BannedUsers.Remove(bannedUser);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        #endregion
    }
}
