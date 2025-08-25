using api.Services;
using CardShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/Sets")]
    public class SetsController : ControllerBase
    {
        private readonly ISetService _setService;
        private readonly ApplicationDbContext _context;

        public SetsController(ISetService setService, ApplicationDbContext context)
        {
            _setService = setService;
            _context = context;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncSets()
        {
            await _setService.SyncSetsAsync();
            return Ok("Sets synchronized with Scryfall.");
        }

        [HttpGet]
        public async Task<IActionResult> GetSets()
        {
            var sets = await _context.Sets.OrderByDescending(s => s.ReleasedAt).ToListAsync();
            return Ok(sets);
        }
    }

}
