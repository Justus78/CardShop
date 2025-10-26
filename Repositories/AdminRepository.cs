using api.DTOs.Account;
using api.DTOs.Order;
using api.Interfaces;
using api.Mappers;
using CardShop.Data;
using CardShop.Mappers;
using CardShop.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories
{
    public class AdminRepository : IAdminService
    {

        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<OrderDto>> GetOrdersForAdminAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return orders.Select(OrderMapper.ToOrderDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return null;
            }

            return order.ToOrderDto();
        }

        public async Task<OrderDto> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null) { return null; }

            return order.ToOrderDto();
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(o => o.Orders)
                .ToListAsync();

            return users.Select(UserMapper.ToUserDto)
                .ToList();
        }        

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            return user;
        }
    } // end interface
} // end namespace
