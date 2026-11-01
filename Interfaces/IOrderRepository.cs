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

        // ── New: for the background expiration sweep ──
        // Finds every order still sitting at Pending whose CreatedDate is older
        // than the cutoff passed in — i.e. "created more than 30 minutes ago and
        // nobody ever completed payment."
        Task<List<Order>> GetStalePendingOrdersAsync(DateTime olderThan);

        // Atomically changes an order from Pending to Cancelled, but ONLY if it is
        // still Pending in the database right now. Returns true if it changed.
        // Used by the sweep so it can't cancel an order that was just paid.
        Task<bool> TryCancelPendingOrderAsync(int orderId);
    }
}