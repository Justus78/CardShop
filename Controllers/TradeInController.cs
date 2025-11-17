using api.DTOs.TradeIn;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers
{
    [ApiController]
    [Route("api/tradeins")]
    [Authorize] // User must be authenticated
    public class TradeInController : ControllerBase
    {
        private readonly ITradeInService _tradeInService;

        public TradeInController(ITradeInService tradeInService)
        {
            _tradeInService = tradeInService;
        }

        // Helper to extract user ID from JWT
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Submit a new trade-in request.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitTradeIn([FromBody] TradeInCreateDto dto)
        {
            var userId = GetUserId();
            var result = await _tradeInService.SubmitTradeInAsync(userId, dto);

            return result is null
                ? BadRequest("Unable to submit trade-in.")
                : Ok(result);
        }

        /// <summary>
        /// Get all trade-ins submitted by the logged-in user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserTradeIns()
        {
            var userId = GetUserId();
            var tradeIns = await _tradeInService.GetUserTradeInsAsync(userId);
            return Ok(tradeIns);
        }

        /// <summary>
        /// Get full details for a single trade-in.
        /// </summary>
        [HttpGet("{tradeInId}")]
        public async Task<IActionResult> GetTradeInById(int tradeInId)
        {
            var userId = GetUserId();
            var tradeIn = await _tradeInService.GetTradeInByIdAsync(userId, tradeInId);

            return tradeIn is null
                ? NotFound("Trade-in not found.")
                : Ok(tradeIn);
        }

        /// <summary>
        /// User cancels their pending trade-in.
        /// Only allowed if status = Pending.
        /// </summary>
        [HttpDelete("{tradeInId}")]
        public async Task<IActionResult> CancelTradeIn(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.CancelTradeInAsync(userId, tradeInId);

            return success ? NoContent() : BadRequest("Trade-in cannot be canceled.");
        }

        /// <summary>
        /// User confirms the final offer and accepts the payout.
        /// </summary>
        [HttpPost("{tradeInId}/confirm")]
        public async Task<IActionResult> ConfirmFinalOffer(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.ConfirmFinalOfferAsync(userId, tradeInId);

            return success ? Ok("Final offer accepted.") : BadRequest("Unable to confirm offer.");
        }

        /// <summary>
        /// User declines the final offer — cards will be returned.
        /// </summary>
        [HttpPost("{tradeInId}/decline")]
        public async Task<IActionResult> DeclineFinalOffer(int tradeInId)
        {
            var userId = GetUserId();
            var success = await _tradeInService.DeclineFinalOfferAsync(userId, tradeInId);

            return success ? Ok("Final offer declined.") : BadRequest("Unable to decline offer.");
        }

        /// <summary>
        /// Get estimated trade-in value before submitting the request.
        /// </summary>
        [HttpPost("estimate")]
        public async Task<IActionResult> EstimateTradeValue([FromBody] List<TradeInItemCreateDto> items)
        {
            var estimate = await _tradeInService.GetEstimatedTradeValueAsync(items);
            return Ok(new { estimatedValue = estimate });
        }
    }
}
