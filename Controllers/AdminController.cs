using api.DTOs.Account;
using api.DTOs.Order;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")] // only admin roles can authorize
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Get orders, one page at a time: api/admin/orders?page=1&pageSize=20&status=Paid
        // Returns { items, totalCount, page, pageSize }. A bad status value gets an
        // automatic 400 from [ApiController].
        [HttpGet("orders")]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetAllOrders([FromQuery] AdminOrderQueryObject query)
        {
            var orders = await _adminService.GetOrdersForAdminAsync(query);
            return Ok(orders);
        }

        // Get specific order by ID
        [HttpGet("orders/{id:int}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var order = await _adminService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound($"Order with ID {id} not found.");

            return Ok(order);
        }

        // Update order status
        // Only Paid <-> Shipped changes are accepted (see OrderService). Anything else
        // throws BusinessRuleException, which the global middleware turns into a 400.
        [HttpPut("orders/update-status")]
        public async Task<ActionResult<OrderDto>> UpdateOrderStatus([FromBody] UpdateOrderStatusDto dto)
        {
            var updatedOrder = await _adminService.UpdateOrderStatusAsync(dto);
            if (updatedOrder == null)
                return NotFound($"Order with ID {dto.OrderId} not found.");

            _logger.LogInformation("Admin {AdminId} set order {OrderId} to {Status}.",
                GetUserId(), dto.OrderId, dto.Status);

            return Ok(updatedOrder);
        }

        // Get users, one page at a time: api/admin/users?page=1&pageSize=20&searchTerm=matt
        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<UserDto>>> GetAllUsers([FromQuery] AdminUserQueryObject query)
        {
            var users = await _adminService.GetAllUsersAsync(query);
            return Ok(users);
        }

        // Get user by ID
        // The declared type was ActionResult<ApplicationUser>, which made Swagger describe
        // the response as the full user entity (password hash, security stamp...). The
        // value actually returned has always been a UserDto, so this just fixes the type.
        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(string id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found.");

            return Ok(user);
        }
    }
}