using api.DTOs.Order;
using api.Models;

namespace api.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId); // creates pending order
        Task<List<OrderDto>> GetOrdersForUserAsync(string userId);
        Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId);
        Task MarkOrderPaidAsync(string paymentIntentId); // webhook finalization
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    }
}
