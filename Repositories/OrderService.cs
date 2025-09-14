using api.DTOs.Order;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using CardShop.Mappers;
using CardShop.Models;
using Microsoft.EntityFrameworkCore;

namespace CardShop.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create pending order linked to PaymentIntent
        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId)
        {
            // Validate stock but don’t deduct yet
            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new Exception($"Product not found: ID {item.ProductId}");

                if (product.StockQuantity < item.Quantity)
                    throw new Exception($"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");
            }

            // Map to order
            var order = OrderMapper.ToOrder(dto, userId);
            order.Status = OrderStatus.Pending;
            order.TotalAmount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return OrderMapper.ToOrderDto(order);
        }

        // Finalize order after Stripe confirms payment
        public async Task MarkOrderPaidAsync(string paymentIntentId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

                    if (order == null)
                        throw new Exception($"Order not found for PaymentIntentId {paymentIntentId}");

                    if (order.Status == OrderStatus.Paid)
                        return; // already processed

                    // Deduct stock + clean up carts
                    foreach (var item in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null) continue;

                        if (product.StockQuantity < item.Quantity)
                            throw new Exception($"Not enough stock for '{product.Name}' at payment time.");

                        product.StockQuantity -= item.Quantity;

                        if (product.StockQuantity == 0)
                        {
                            var cartItems = await _context.CartItems
                                .Where(ci => ci.ProductId == product.Id && ci.UserId != order.UserId)
                                .ToListAsync();

                            _context.CartItems.RemoveRange(cartItems);
                        }
                    }

                    order.Status = OrderStatus.Paid;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return orders.Select(OrderMapper.ToOrderDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order == null ? null : OrderMapper.ToOrderDto(order);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
