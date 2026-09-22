using CardShop.Models;

namespace api.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order);
        Task<Order?> GetByIdAsync(int orderId, string userId);
        Task<Order?> GetByIdAsync(int orderId); // admin/webhook use, no user scoping
        Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId);
        Task<List<Order>> GetForUserAsync(string userId);
        Task<bool> SaveChangesAsync();
        Task RemoveStaleCartItemsAsync(int productId, string excludeUserId);
    }
}