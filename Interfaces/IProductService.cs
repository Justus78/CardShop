using api.DTOs.Product;
using api.Helpers;
using CardShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync(ProductQueryObject query);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(CreateProductDto dto);
        Task<Product?> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync(); // For pagination metadata
    }
}
