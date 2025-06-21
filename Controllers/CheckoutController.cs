using api.DTOs.Order;
using api.DTOs.Stripe;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ICheckoutService checkoutService, UserManager<ApplicationUser> userManager)
        {
            _checkoutService = checkoutService;
            _userManager = userManager;
        }

        [HttpPost("checkout")]
        public async Task<ActionResult<StripePaymentResultDto>> Checkout([FromBody] CreateOrderDto orderDto)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var result = await _checkoutService.CreatePaymentAndOrderAsync(orderDto, userId);
            return Ok(result);
        }

    } // end controller
}
