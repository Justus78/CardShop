using api.DTOs.Order;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Limited to 5 requests per 10 minutes per user. Each call reserves stock and
        // creates a Stripe PaymentIntent. Too many unpaid orders at once (3) is
        // refused separately, inside OrderService.
        [EnableRateLimiting(RateLimitingExtensions.OrderCreationPolicy)]
        [HttpPost("create-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateOrderDto dto)
        {
            var userId = GetUserId();
            var result = await _orderService.CreateOrderWithPaymentIntentAsync(dto, userId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetOrdersForUserAsync(GetUserId());
            return Ok(orders);
        }

        // Uses the user-scoped lookup, so a customer can only ever fetch their own orders.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id, GetUserId());
            if (order == null) return NotFound();
            return Ok(order);
        }
    }
}