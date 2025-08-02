using api.DTOs.Order;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CardShop.Controllers
{
    [ApiController]
    [Route("api/order")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // POST: api/order
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var order = await _orderService.CreateOrderAsync(dto, userId);
            return Ok(order);
        }

        // GET: api/order
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<OrderDto>>> GetUserOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var orders = await _orderService.GetOrdersForUserAsync(userId);
            return Ok(orders);
        }

        // GET: api/order/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var order = await _orderService.GetOrderByIdAsync(id, userId);
            if (order == null) return NotFound();

            return Ok(order);
        }

        

    } // end controller
} // end namespace
