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
        public async Task<IActionResult> AddDraftItem([FromForm] TradeInItemCreateDto dto)
        {
            var userId = GetUserId();
            var draft = await _tradeInService.AddItemToDraftAsync(userId, dto);
            return Ok(draft);
        }

        [HttpDelete("draft/items/{itemId:int}")]
        public async Task<IActionResult> RemoveDraftItem(int itemId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.RemoveItemFromDraftAsync(userId, itemId);
            return success ? NoContent() : BadRequest("Unable to remove item.");
        }

        [HttpPost("draft/submit/{tradeInId:int}")]
        public async Task<IActionResult> SubmitDraftTradeIn(int tradeInId)
        {
            var userId = GetUserId();
            var result = await _tradeInService.SubmitDraftAsync(tradeInId);
            return result == null ? BadRequest("Draft submission failed.") : Ok(result);
        }

        [HttpDelete("draft/deleteDraft/{tradeInId:int}")]
        
        public async Task<IActionResult> DeleteTradeIn(int tradeInId)
        {
            var userId = GetUserId();
            var draftTradeIn = await _tradeInService.GetTradeInByIdAsync(tradeInId);

            if (draftTradeIn == null)
                return NotFound();

            var result = await _tradeInService.CancelTradeInAsync(userId, draftTradeIn.Id);

            return Ok(result);
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

        [HttpGet("{tradeInId:int}")]
        public async Task<IActionResult> GetTradeInById(int tradeInId)
        {
            var userId = GetUserId();
            var tradeIn = await _tradeInService.GetTradeInByIdAsync(tradeInId);
            return tradeIn == null ? NotFound("Trade-in not found.") : Ok(tradeIn);
        }

        [HttpPatch("{tradeInId:int}")]
        public async Task<IActionResult> ReturnTradeIn(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.ReturnTradeInAsync(userId, tradeInId);
            return success ? NoContent() : BadRequest("Trade-in cannot be canceled.");
        }

        [HttpPost("{tradeInId:int}/confirm")]
        public async Task<IActionResult> ConfirmFinalOffer(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.ConfirmFinalOfferAsync(userId, tradeInId);
            return success ? Ok("Final offer accepted.") : BadRequest("Unable to confirm offer.");
        }

        [HttpPost("{tradeInId:int}/decline")]
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
