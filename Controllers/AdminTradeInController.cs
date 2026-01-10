using api.DTOs.TradeIn;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin/tradeins")]
    [Authorize(Roles = "Admin")]
    public class AdminTradeInController : ControllerBase
    {
        private readonly IAdminTradeInService _adminTradeInService;

        public AdminTradeInController(IAdminTradeInService adminTradeInService)
        {
            _adminTradeInService = adminTradeInService;
        }

        /// <summary>
        /// Get all trade-ins, optionally filtered by status.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTradeIns([FromQuery] string? status)
        {
            var list = await _adminTradeInService.GetAllTradeInsAsync(status);
            return Ok(list);
        }

        /// <summary>
        /// Get full details for a single trade-in.
        /// </summary>
        [HttpGet("{tradeInId}")]
        public async Task<IActionResult> GetTradeInDetails(int tradeInId)
        {
            var details = await _adminTradeInService.GetTradeInDetailsAsync(tradeInId);
            return details is null ? NotFound("Trade-in not found.") : Ok(details);
        }

        /// <summary>
        /// Change the status of a trade-in (validates allowed transitions).
        /// </summary>
        [HttpPatch("{tradeInId}/status")]
        public async Task<IActionResult> UpdateTradeInStatus(int tradeInId, [FromBody] UpdateTradeInStatusDto dto)
        {
            if (dto == null) return BadRequest("Payload required.");

            var success = await _adminTradeInService.UpdateTradeInStatusAsync(tradeInId, dto.Status);
            if (!success) return BadRequest("Invalid status transition or trade-in not found.");

            return Ok("Status updated.");
        }

        /// <summary>
        /// Update the final approved value for a specific trade-in item.
        /// </summary>
        [HttpPatch("items/{itemId}/finalValue")]
        public async Task<IActionResult> UpdateItemFinalValue(int itemId, [FromBody] UpdateTradeInItemValueDto dto)
        {
            if (dto == null) return BadRequest("Payload required.");
            if (dto.FinalUnitValue < 0) return BadRequest("Final value must be >= 0.");

            var success = await _adminTradeInService.UpdateItemFinalValueAsync(itemId, dto.FinalUnitValue);
            if (!success) return NotFound("Item not found or could not be updated.");

            return Ok(new
            {
                itemId,
                finalUnitValue = dto.FinalUnitValue
            });
        }

        /// <summary>
        /// Admin submits a final offer to the user (computes final value from item values).
        /// </summary>
        [HttpPost("{tradeInId}/submit-final-offer")]
        public async Task<IActionResult> SubmitFinalOffer(int tradeInId)
        {
            var success = await _adminTradeInService.SubmitFinalOfferAsync(tradeInId);
            return success ? Ok("Final offer submitted.") : BadRequest("Unable to submit offer.");
        }

        /// <summary>
        /// Apply credit to the user's account after the user accepts the offer.
        /// This is idempotent (won't credit twice).
        /// </summary>
        [HttpPost("{tradeInId}/credit")]
        public async Task<IActionResult> CreditUserAccount(int tradeInId)
        {
            try
            {
                var success = await _adminTradeInService.CreditUserAccountAsync(tradeInId);
                return success ? Ok("Account credited.") : BadRequest("Unable to credit user (missing final value, already credited, or not found).");
            }
            catch (Exception ex)
            {
                // Log exception as appropriate (not included here)
                return StatusCode(500, $"Server error while crediting account: {ex.Message}");
            }
        }

        /// <summary>
        /// Mark that cards were returned to the user after they decline the final offer.
        /// </summary>
        [HttpPost("{tradeInId}/return")]
        public async Task<IActionResult> ReturnCards(int tradeInId)
        {
            var success = await _adminTradeInService.ReturnCardsToUserAsync(tradeInId);
            return success ? Ok("Cards returned to user.") : BadRequest("Unable to return cards (maybe already returned or credited).");
        }
    }
}
