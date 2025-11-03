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

        // Get all orders for admin
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

        // Get single order by ID
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return null;

            return OrderMapper.ToOrderDto(order);
        }

        // Update order status
        public async Task<OrderDto?> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                return null;

            // Update the order status
            order.Status = dto.Status;

            // Save changes to database
            await _context.SaveChangesAsync();

            // Return updated DTO
            return OrderMapper.ToOrderDto(order);
        }

        // Get all users
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Orders)
                .ToListAsync();

            return users.Select(UserMapper.ToUserDto).ToList();
        }

        // Get user by ID
        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            return UserMapper.ToUserDto(user); // returns null if not found
        }
    }
}
