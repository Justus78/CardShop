using api.DTOs.Order;
using api.Models;
using CardShop.Models;

namespace api.Interfaces
{
    public interface IOrderService
    {
        Task<CreateOrderResultDto> CreateOrderWithPaymentIntentAsync(CreateOrderDto dto, string userId);
        Task<List<OrderDto>> GetOrdersForUserAsync(string userId);
        Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId);
        Task MarkOrderPaidAsync(string paymentIntentId);
        Task MarkOrderFailedAsync(string paymentIntentId);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    }
}
