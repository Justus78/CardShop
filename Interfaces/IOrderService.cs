using api.DTOs.Order;

namespace api.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(string userId);
        Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    }
}
