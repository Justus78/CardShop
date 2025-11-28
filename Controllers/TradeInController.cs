using api.DTOs.TradeIn;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers
{
    [ApiController]
    [Route("api/tradeins")]
    [Authorize]
    public class TradeInController : ControllerBase
    {
        private readonly ITradeInService _tradeInService;

        public TradeInController(ITradeInService tradeInService)
        {
            _tradeInService = tradeInService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ----------------------------
        // Draft / Persistent trade-ins
        // ----------------------------

        [HttpGet("draft")]
        public async Task<IActionResult> GetDraftTradeIn()
        {
            var userId = GetUserId();
            var draft = await _tradeInService.GetOrCreateDraftAsync(userId);
            return Ok(draft);
        }

        [HttpPost("draft/items")]
        public async Task<IActionResult> AddDraftItem([FromBody] TradeInItemCreateDto dto)
        {
            var userId = GetUserId();
            var draft = await _tradeInService.AddItemToDraftAsync(userId, dto);
            return Ok(draft);
        }

        [HttpDelete("draft/items/{itemId}")]
        public async Task<IActionResult> RemoveDraftItem(int itemId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.RemoveItemFromDraftAsync(userId, itemId);
            return success ? NoContent() : BadRequest("Unable to remove item.");
        }

        [HttpPost("draft/submit")]
        public async Task<IActionResult> SubmitDraftTradeIn()
        {
            var userId = GetUserId();
            var result = await _tradeInService.SubmitDraftAsync(userId);
            return result == null ? BadRequest("Draft submission failed.") : Ok(result);
        }

        // ----------------------------
        // Submitted trade-ins
        // ----------------------------

        [HttpGet]
        public async Task<IActionResult> GetUserTradeIns()
        {
            var userId = GetUserId();
            var tradeIns = await _tradeInService.GetUserTradeInsAsync(userId);
            return Ok(tradeIns);
        }

        [HttpGet("{tradeInId}")]
        public async Task<IActionResult> GetTradeInById(int tradeInId)
        {
            var userId = GetUserId();
            var tradeIn = await _tradeInService.GetTradeInByIdAsync(userId, tradeInId);
            return tradeIn == null ? NotFound("Trade-in not found.") : Ok(tradeIn);
        }

        [HttpDelete("{tradeInId}")]
        public async Task<IActionResult> CancelTradeIn(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.CancelTradeInAsync(userId, tradeInId);
            return success ? NoContent() : BadRequest("Trade-in cannot be canceled.");
        }

        [HttpPost("{tradeInId}/confirm")]
        public async Task<IActionResult> ConfirmFinalOffer(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.ConfirmFinalOfferAsync(userId, tradeInId);
            return success ? Ok("Final offer accepted.") : BadRequest("Unable to confirm offer.");
        }

        [HttpPost("{tradeInId}/decline")]
        public async Task<IActionResult> DeclineFinalOffer(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.DeclineFinalOfferAsync(userId, tradeInId);
            return success ? Ok("Final offer declined.") : BadRequest("Unable to decline offer.");
        }

        [HttpPost("estimate")]
        public async Task<IActionResult> EstimateTradeValue([FromBody] List<TradeInItemCreateDto> items)
        {
            var estimate = await _tradeInService.GetEstimatedTradeValueAsync(items);
            return Ok(new { estimatedValue = estimate });
        }
    }
}
