using api.Extensions;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace api.Controllers
{
    [ApiController]
    [Route("api/sets")]
    // ADMIN-ONLY BY DEFAULT: every action in this controller requires the Admin role
    // unless it is explicitly marked [AllowAnonymous]. That way an action added later
    // is protected without anyone having to remember to protect it.
    [Authorize(Roles = "Admin")]
    public class SetsController : ControllerBase
    {
        private readonly ISetService _setService;
        private readonly ILogger<SetsController> _logger;

        // The controller no longer takes ApplicationDbContext: all data access
        // goes through the service, like the rest of the API.
        public SetsController(ISetService setService, ILogger<SetsController> logger)
        {
            _setService = setService;
            _logger = logger;
        }

        // A sync calls Scryfall and rewrites about a thousand rows, so it is admin-only,
        // logged with who started it, and the service refuses to run two at once (409).
        // If Scryfall is unreachable the service throws a 502 with a clear message.
        [HttpPost("sync")]
        public async Task<IActionResult> SyncSets()
        {
            _logger.LogInformation("Admin {AdminId} started a Scryfall set sync.",
                User.FindFirstValue(ClaimTypes.NameIdentifier));

            await _setService.SyncSetsAsync();
            return Ok("Sets synchronized with Scryfall.");
        }

        // PUBLIC: visitors browse and filter by set before they log in. [AllowAnonymous]
        // overrides the class-level Admin requirement for this one action. The list is
        // cached in SetService, so anonymous traffic doesn't hit the database each time,
        // and limited to 120 requests per minute per IP.
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.PublicReadsPolicy)]
        [HttpGet]
        public async Task<IActionResult> GetSets()
        {
            var sets = await _setService.GetAllAsync();
            return Ok(sets);
        }
    }

}