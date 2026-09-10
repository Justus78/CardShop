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
    }
}
