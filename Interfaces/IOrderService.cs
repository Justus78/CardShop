using api.DTOs.Order;

namespace api.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId, string? transactionId = null);
        Task<List<OrderDto>> GetOrdersForUserAsync(string userId);
        Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId);
    }
}
