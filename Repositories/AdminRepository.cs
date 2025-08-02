using api.DTOs.Account;
using api.DTOs.Order;
using api.Interfaces;
using api.Mappers;
using CardShop.Data;
using CardShop.Mappers;
using Microsoft.AspNetCore.Mvc;
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
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(OrderMapper.ToOrderDto).ToList();
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(o => o.Orders)
                .ToListAsync();

            return users.Select(UserMapper.ToUserDto)
                .ToList();
        }
    } // end interface
} // end namespace
