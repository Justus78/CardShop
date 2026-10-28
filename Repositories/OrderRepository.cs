using api.Interfaces;
using api.Models;
using CardShop.Data;
using CardShop.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // reload with related data so callers (service -> mapper) never hit nulls
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstAsync(o => o.Id == order.Id);
        }

        public async Task<Order?> GetByIdAsync(int orderId, string userId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);
        }

        public async Task<List<Order>> GetForUserAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

        public async Task RemoveStaleCartItemsAsync(int productId, string excludeUserId)
        {
            var cartItems = await _context.CartItems
                .Where(ci => ci.ProductId == productId && ci.UserId != excludeUserId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
        }

        // Include(o => o.OrderItems) matters here: the background service needs each
        // stale order's line items (ProductId + Quantity) to know what stock to
        // restore. Without the Include, OrderItems would be empty/not loaded and
        // the restore loop in OrderService would silently do nothing.
        public async Task<List<Order>> GetStalePendingOrdersAsync(DateTime olderThan)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Status == OrderStatus.Pending && o.CreatedDate < olderThan)
                .ToListAsync();
        }
    }
}