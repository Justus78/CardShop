using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using api.Services;
using CardShop.Data;
using CardShop.Models;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using static api.Enums.ProductEnums;

namespace api.Repositories
{
    public class ProductRepository : IProductService
    {

        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService;

        public ProductRepository(ApplicationDbContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
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

            if (productModel == null) 
            { 
                return null; 
            }

            // check for product image
            if (productModel.CloudinaryId != null) // if product has an image on cloudinary
            {
                try
                {
                    // try to delete the image url
                    var result = await _photoService.DeletePhotoAsync(productModel.CloudinaryId);

                    // Check if the deletion was successful
                    if (result.Result != "ok")
                    {
                        Console.WriteLine($"Failed to delete photo: {productModel.CloudinaryId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting photo: {ex.Message}");
                }
            }

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

            // check for new photo
            if (dto.ProductImage != null)
            {
                if (product.CloudinaryId != null)
                { // if there is already a pic
                    await _photoService.DeletePhotoAsync(product.CloudinaryId); // delete current photo
                }
                // send new photo to cloudinary
                var result = await _photoService.AddPhotoAsync(dto.ProductImage);
                product.ImageUrl = result.Url.ToString(); // add new url to players
                product.CloudinaryId = result.PublicId.ToString();
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
