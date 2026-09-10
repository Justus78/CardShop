using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using CardShop.Models;
using Microsoft.EntityFrameworkCore;
using static api.Enums.ProductEnums;

namespace api.Repositories 
{ 
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync() => await _context.Products.CountAsync();

        public async Task<Product> AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> DeleteAsync(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return null;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<PagedResult<Product>> GetAllAsync(ProductQueryObject queryObject)
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.CardDetails)
                .Include(p => p.SealedProductDetails)
                .Include(p => p.AccessoryDetails);

            /* ---------- Filtering ---------- */

            if (queryObject.Category.HasValue)
                query = query.Where(p => p.ProductCategory == queryObject.Category.Value);

            if (queryObject.MinPrice.HasValue)
                query = query.Where(p => p.Price >= queryObject.MinPrice.Value);

            if (queryObject.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= queryObject.MaxPrice.Value);

            if (queryObject.InStockOnly == true)
                query = query.Where(p => p.StockQuantity > 0);

            if (!string.IsNullOrWhiteSpace(queryObject.SearchTerm))
                query = query.Where(p =>
                    p.Name!.Contains(queryObject.SearchTerm) ||
                    p.Description!.Contains(queryObject.SearchTerm));

            // Card-specific filters — EF translates these into a join against CardDetails automatically
            if (queryObject.Rarity.HasValue)
                query = query.Where(p => p.CardDetails != null && p.CardDetails.CardRarity == queryObject.Rarity.Value);

            if (queryObject.IsFoil.HasValue)
                query = query.Where(p => p.CardDetails != null && p.CardDetails.IsFoil == queryObject.IsFoil.Value);

            /* ---------- Sorting ---------- */

            query = queryObject.SortBy?.ToLower() switch
            {
                "name" => queryObject.Ascending
                    ? query.OrderBy(p => p.Name)
                    : query.OrderByDescending(p => p.Name),

                "price" => queryObject.Ascending
                    ? query.OrderBy(p => p.Price)
                    : query.OrderByDescending(p => p.Price),

                "category" => queryObject.Ascending
                    ? query.OrderBy(p => p.ProductCategory)
                    : query.OrderByDescending(p => p.ProductCategory),

                _ => queryObject.Ascending
                    ? query.OrderBy(p => p.Id)
                    : query.OrderByDescending(p => p.Id)
            };

            /* ---------- Pagination ---------- */

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((queryObject.Page - 1) * queryObject.PageSize)
                .Take(queryObject.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                Page = queryObject.Page,
                PageSize = queryObject.PageSize
            };
        } // end get all

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.CardDetails)
                .Include(p => p.SealedProductDetails)
                .Include(p => p.AccessoryDetails)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    }
}


