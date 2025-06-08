using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using CardShop.Data;
using CardShop.Models;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using static api.Enums.ProductEnums;

namespace api.Repositories
{
    public class ProductRepository : IProductService
    {

        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        } // end constructor


        public Task<int> CountAsync()
        {
            throw new NotImplementedException();
        } // end count

        public async Task<Product> CreateAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
            
            
        } // end create

        public async Task<Product?> DeleteAsync(int id)
        {
            var productModel = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (productModel != null) { return null; }

            _context.Products.Remove(productModel);
            await _context.SaveChangesAsync();
            return productModel;
        } // end delete

        public async Task<IEnumerable<Product>> GetAllAsync(ProductQueryObject queryObject)
        {
            IQueryable<Product> query = _context.Products;

            /* ---------- Filtering ---------- */

            // ProductCategory (enum)
            if (!string.IsNullOrEmpty(queryObject.Category))
            {
                if (Enum.TryParse<ProductCategory>(queryObject.Category, true, out var categoryEnum))
                {
                    query = query.Where(p => p.ProductCategory == categoryEnum);
                }
            }

            // CardRarity (nullable enum)
            if (!string.IsNullOrEmpty(queryObject.Rarity))
            {
                if (Enum.TryParse<CardRarity>(queryObject.Rarity, true, out var rarityEnum))
                {
                    query = query.Where(p => p.CardRarity == rarityEnum);
                }
            }

            // IsFoil (bool)
            if (queryObject.IsFoil.HasValue)
            {
                query = query.Where(p => p.IsFoil == queryObject.IsFoil.Value);
            }

            /* ---------- Sorting ---------- */
            query = queryObject.SortBy?.ToLower() switch
            {
                "name" => queryObject.Ascending ? query.OrderBy(p => p.Name)
                                                    : query.OrderByDescending(p => p.Name),

                "price" => queryObject.Ascending ? query.OrderBy(p => p.Price)
                                                    : query.OrderByDescending(p => p.Price),

                "category" => queryObject.Ascending ? query.OrderBy(p => p.ProductCategory)
                                                    : query.OrderByDescending(p => p.ProductCategory),

                "rarity" => queryObject.Ascending ? query.OrderBy(p => p.CardRarity)
                                                    : query.OrderByDescending(p => p.CardRarity),

                _ => queryObject.Ascending ? query.OrderBy(p => p.Id)
                                                    : query.OrderByDescending(p => p.Id)
            };

            /* ---------- Pagination ---------- */
            query = query
                .Skip((queryObject.Page - 1) * queryObject.PageSize)
                .Take(queryObject.PageSize);

            return await query.ToListAsync();  

        }// end get all products

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        } // end get by id

        public async Task<Product?> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            { 
                return null;
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.ProductCategory = dto.ProductCategory;
            product.IsFoil = dto.IsFoil;
            product.CardCondition = dto.CardCondition;
            product.CardRarity = dto.CardRarity;
            product.CardType = dto.CardType;
            product.CollectionNumber = dto.CollectionNumber;
            product.SetName = dto.SetName;

            await _context.SaveChangesAsync();

            return product;
        } // end update
    } // end repo
} // end namespace
