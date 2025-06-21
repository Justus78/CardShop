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
            var order = OrderMapper.ToOrder(dto, userId);

            if (!string.IsNullOrEmpty(transactionId))
                order.TransactionId = transactionId; // add the transaction id from stripe to the order

            order.TotalAmount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var createdOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return OrderMapper.ToOrderDto(createdOrder!);
        }

        public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
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
