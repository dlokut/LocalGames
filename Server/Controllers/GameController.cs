using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : Controller
    {
        private readonly IgdbManager igdbManager;

        public GameController()
        {
            string? clientId = Environment.GetEnvironmentVariable("IGDB_CLIENT_ID");
            string? clientSecret = Environment.GetEnvironmentVariable("IGDB_CLIENT_SECRET");

            if (clientId == null || clientSecret == null)
            {
                throw new Exception("Client id or client secret is not set");
            }

            igdbManager = new IgdbManager(clientId, clientSecret);
        }

        [HttpGet]
        [Route("v1/GetGameId")]
        public async Task<ActionResult> GetGameIdAsync(string name)
        {
            long? gameId = await igdbManager.GetGameIdAsync(name);

            return Ok(gameId);
        }
    }
}
