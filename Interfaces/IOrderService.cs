using api.DTOs.Order;
using api.Models;

namespace api.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId, string? transactionId = null);
        Task<List<OrderDto>> GetOrdersForUserAsync(string userId);
        Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId);
        Task MarkOrderPaidAsync(string paymentIntentId);
        Task<Bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        
    }
}
