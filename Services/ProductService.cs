using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using CardShop.Models;
using static api.Enums.ProductEnums;

namespace api.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IPhotoService _photoService;

        public ProductService(IProductRepository repository, IPhotoService photoService)
        {
            _repository = repository;
            _photoService = photoService;
        }

        public Task<int> CountAsync() => _repository.CountAsync();
        public Task<PagedResult<Product>> GetAllAsync(ProductQueryObject queryObject) => _repository.GetAllAsync(queryObject); public Task<Product?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<Product> CreateAsync(CreateProductDto dto)
        {
            var product = dto.ToProduct();

            // image handling now lives here too, alongside the rest of creation logic
            product.ImageUrl = dto.ProductImage;

            ProductValidator.ValidateProductDetails(product);
            return await _repository.AddAsync(product);
        }

        public async Task<Product?> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.BestSeller = dto.BestSeller ?? product.BestSeller;

            switch (product.ProductCategory)
            {
                case ProductCategory.Card when dto.CardDetail != null:
                    product.CardDetails ??= new CardDetail();
                    product.CardDetails.IsFoil = dto.CardDetail.IsFoil;
                    product.CardDetails.CardCondition = dto.CardDetail.CardCondition;
                    product.CardDetails.CardRarity = dto.CardDetail.CardRarity;
                    product.CardDetails.CardType = dto.CardDetail.CardType;
                    product.CardDetails.CollectionNumber = dto.CardDetail.CollectionNumber;
                    product.CardDetails.SetName = dto.CardDetail.SetName;
                    break;

                case ProductCategory.Sealed when dto.SealedProductDetail != null:
                    product.SealedProductDetails ??= new SealedProductDetail();
                    product.SealedProductDetails.SetName = dto.SealedProductDetail.SetName;
                    product.SealedProductDetails.SealedProductType = dto.SealedProductDetail.SealedProductType;
                    product.SealedProductDetails.Language = dto.SealedProductDetail.Language;
                    break;

                case ProductCategory.Accessory when dto.AccessoryDetail != null:
                    product.AccessoryDetails ??= new AccessoryDetail();
                    product.AccessoryDetails.Brand = dto.AccessoryDetail.Brand;
                    product.AccessoryDetails.AccessoryCategory = dto.AccessoryDetail.AccessoryCategory;
                    product.AccessoryDetails.Dimensions = dto.AccessoryDetail.Dimensions;
                    break;
            }

            ProductValidator.ValidateProductDetails(product);
            await _repository.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> DeleteAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            if (product.CloudinaryId != null)
            {
                try
                {
                    var result = await _photoService.DeletePhotoAsync(product.CloudinaryId);
                    if (result.Result != "ok")
                        Console.WriteLine($"Failed to delete photo: {product.CloudinaryId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting photo: {ex.Message}");
                }
            }

            return await _repository.DeleteAsync(id);
        }
    }
}