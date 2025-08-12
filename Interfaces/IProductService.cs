using api.DTOs.Product;
using api.Helpers;
using CardShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product product);
        Task<Product?> UpdateAsync(int id, UpdateProductDto dto);
        Task<Product?> DeleteAsync(int id);
        Task<int> CountAsync(); // For pagination metadata
    }
}
