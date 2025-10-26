using api.DTOs.Account;
using api.DTOs.Order;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")] // only admin roles can authorize
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // Get all orders
        [HttpGet("orders")]
        public async Task<ActionResult<List<OrderDto>>> GetAllOrders()
        {
            var orders = await _adminService.GetOrdersForAdminAsync();
            return Ok(orders);
        }

        // Get specific order by ID
        [HttpGet("orders/{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var order = await _adminService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound($"Order with ID {id} not found.");

            return Ok(order);
        }

        // Update order status
        [HttpPut("orders/update-status")]
        public async Task<ActionResult<OrderDto>> UpdateOrderStatus([FromBody] UpdateOrderStatusDto dto)
        {
            var updatedOrder = await _adminService.UpdateOrderStatusAsync(dto);
            if (updatedOrder == null)
                return NotFound($"Order with ID {dto.OrderId} not found.");

            return Ok(updatedOrder);
        }

        // Get all users
        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        // Get user by ID
        [HttpGet("users/{id}")]
        public async Task<ActionResult<ApplicationUser>> GetUserById(string id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found.");

            return Ok(user);
        }
    }
}
