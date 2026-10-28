using api.DTOs.Product;
using api.Helpers;
using CardShop.Models;

namespace api.Interfaces
{
    public interface IProductRepository
    {
        Task<PagedResult<Product>> GetAllAsync(ProductQueryObject queryObject);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<Product?> DeleteAsync(int id);
        Task<int> CountAsync();
        Task<bool> SaveChangesAsync();

        // ── New: atomic stock operations ──
        // These do NOT go through the normal EF change-tracking + SaveChangesAsync
        // flow. They execute as a single SQL UPDATE statement immediately, which is
        // what makes TryDecrementStockAsync safe against two concurrent buyers —
        // see the implementation comments for why.

        // Attempts to decrement stock by `quantity`, but ONLY if current stock is
        // enough to cover it. Returns true if the decrement happened, false if there
        // wasn't enough stock (in which case nothing was changed).
        Task<bool> TryDecrementStockAsync(int productId, int quantity);

        // Adds `quantity` back to stock. Used when an order fails, gets cancelled,
        // or expires after reserving stock at creation time.
        Task RestoreStockAsync(int productId, int quantity);
    }
}