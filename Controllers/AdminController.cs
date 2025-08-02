using api.DTOs.Account;
using api.DTOs.Order;
using api.Interfaces;
using api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminRepo;

        public AdminController(IAdminService adminService)
        {
            _adminRepo = adminService;
        }

        // GET
        [HttpGet("get-orders")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<OrderDto>>> GetAllOrders()
        {
            var orders = await _adminRepo.GetOrdersForAdminAsync();
            return Ok(orders);
        } // end get orders for admin

        //GET
        [HttpGet("get-users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<UserDto>>> GetALlUsers()
        {
            var users = await _adminRepo.GetAllUsersAsync();
            return Ok(users);
        } // end get users for admin
    }
}
