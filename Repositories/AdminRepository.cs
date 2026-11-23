using api.DTOs.Account;
using api.DTOs.Order;
using api.Helpers;
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
        private readonly IOrderService _orderService;

        public AdminRepository(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        // Get all orders for admin
        // AsNoTracking: these admin reads never modify what they load, so EF doesn't need
        // to track every order, item and product it returns.
        public async Task<PagedResult<OrderDto>> GetOrdersForAdminAsync(AdminOrderQueryObject query)
        {
            // Filter first, WITHOUT the Includes: counting only needs the Orders table.
            var baseQuery = _context.Orders.AsNoTracking();

            if (query.Status.HasValue)
                baseQuery = baseQuery.Where(o => o.Status == query.Status.Value);

            var totalCount = await baseQuery.CountAsync();

            // Then load just the requested page, with the related data it needs.
            // The Id tie-breaker keeps the order stable: two orders created at the same
            // moment can't swap places between page 1 and page 2.
            var orders = await baseQuery
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedDate)
                .ThenByDescending(o => o.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<OrderDto>
            {
                Items = orders.Select(OrderMapper.ToOrderDto).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // Get single order by ID
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return null;

            return OrderMapper.ToOrderDto(order);
        }

        // Update order status
        // The rules for WHICH status changes are allowed live in OrderService, so this
        // method and any other caller share one set of rules. (Writing the status
        // directly here let an admin cancel an order without its reserved stock
        // being released.) An invalid change throws BusinessRuleException -> 400.
        public async Task<OrderDto?> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
        {
            var updated = await _orderService.UpdateOrderStatusAsync(dto.OrderId, dto.Status);
            if (!updated)
                return null;

            // Re-read for the response (untracked, so it reflects what is in the database).
            return await GetOrderByIdAsync(dto.OrderId);
        }

        // Get all users
        public async Task<PagedResult<UserDto>> GetAllUsersAsync(AdminUserQueryObject query)
        {
            var baseQuery = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                baseQuery = baseQuery.Where(u => u.UserName!.Contains(term) || u.Email!.Contains(term));
            }

            var totalCount = await baseQuery.CountAsync();

            // Only this page's users get their orders loaded, not every user's.
            var users = await baseQuery
                .Include(u => u.Orders)
                .OrderBy(u => u.UserName)
                .ThenBy(u => u.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<UserDto>
            {
                Items = users.Select(UserMapper.ToUserDto).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // Get user by ID
        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            return UserMapper.ToUserDto(user); // returns null if not found
        }
    }
}