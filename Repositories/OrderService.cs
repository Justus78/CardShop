using api.DTOs.Order;
using api.Interfaces;
using CardShop.Data;
using CardShop.Mappers;
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

        

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId, string? transactionId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate stock
                foreach (var item in dto.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"Product not found: ID {item.ProductId}");

                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }

                // 2. Create order
                var order = OrderMapper.ToOrder(dto, userId);
                if (!string.IsNullOrEmpty(transactionId))
                    order.TransactionId = transactionId;

                order.TotalAmount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
                _context.Orders.Add(order);

                // 3. Adjust stock and clean up CartItems
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    product.StockQuantity -= item.Quantity;

                    if (product.StockQuantity == 0)
                    {
                        // Remove from all other users' cart items
                        var cartItems = await _context.CartItems
                            .Where(ci => ci.ProductId == product.Id && ci.UserId != userId)
                            .ToListAsync();

                        _context.CartItems.RemoveRange(cartItems);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var createdOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                return OrderMapper.ToOrderDto(createdOrder!);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
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
    }
}
