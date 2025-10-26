using api.DTOs.Order;
using api.Interfaces;
using api.Models;
using CardShop.Data;
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

        // Create a pending order (called before Stripe payment)
        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Validate stock
                foreach (var item in dto.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"Product not found: {item.ProductId}");

                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }

                // Map to order
                var order = new Order
                {
                    UserId = userId,
                    Status = OrderStatus.Pending,
                    PaymentIntentId = dto.PaymentIntentId,
                    RecipientName = dto.ShippingInfo.FullName,
                    Street = dto.ShippingInfo.Address,
                    City = dto.ShippingInfo.City,
                    State = dto.ShippingInfo.State,
                    PostalCode = dto.ShippingInfo.PostalCode,
                    Country = dto.ShippingInfo.Country,
                    OrderItems = dto.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                    TotalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToDto(order);
            });
        }

        // Called by Stripe webhook when payment succeeds
        public async Task MarkOrderPaidAsync(string paymentIntentId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

                if (order == null) return;

                if (order.Status == OrderStatus.Paid) return;

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
                order.PaidAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }

        // Called by stripe webhook when payment fails
        public async Task MarkOrderFailedAsync(string paymentIntentId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

            if (order == null) return;

            if (order.Status == OrderStatus.Failed || order.Status == OrderStatus.Paid) return;

            order.Status = OrderStatus.Failed;
            await _context.SaveChangesAsync();
        }


        public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order == null ? null : MapToDto(order);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                Username = order.User.UserName,
                PaymentIntentId = order.PaymentIntentId,
                Status = order.Status,
                CreatedAt = (DateTime)order.CreatedDate,
                PaidAt = order.PaidAt,
                RecipientName = order.RecipientName,
                Street = order.Street,
                City = order.City,
                State = order.State,
                PostalCode = order.PostalCode,
                Country = order.Country,
                TotalAmount = (decimal)order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
    }
}
