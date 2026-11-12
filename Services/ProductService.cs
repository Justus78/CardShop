using api.DTOs.Product;
using api.Exceptions;
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
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository repository, IPhotoService photoService, ILogger<ProductService> logger)
        {
            _repository = repository;
            _photoService = photoService;
            _logger = logger;
        }

        public Task<int> CountAsync() => _repository.CountAsync();
        public Task<PagedResult<Product>> GetAllAsync(ProductQueryObject queryObject) => _repository.GetAllAsync(queryObject);
        public Task<Product?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<Product> CreateAsync(CreateProductDto dto)
        {
            var product = dto.ToProduct();

            // image handling now lives here too, alongside the rest of creation logic
            product.ImageUrl = dto.ProductImage;

            ValidatePriceAndStock(product.Price, product.StockQuantity);
            ProductValidator.ValidateProductDetails(product);
            return await _repository.AddAsync(product);
        }

        public async Task<Product?> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            ValidatePriceAndStock(dto.Price, dto.StockQuantity);

            product.Price = dto.Price;

            // NOTE: this overwrites stock with whatever the edit form sent, which can undo
            // stock reserved by orders placed after the admin opened the form. See the
            // walkthrough: stock should become its own atomic operation.
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

            // A product that appears in any order can't be deleted: depending on how the
            // database relationship is set up, that would either erase lines from past
            // orders (cascade) or fail with a foreign key error. ConflictException becomes
            // a 409 through the global exception middleware.
            if (await _repository.HasOrderHistoryAsync(id))
            {
                throw new ConflictException(
                    "This product appears in existing orders and cannot be deleted. Set its stock to 0 to stop selling it.");
            }

            var cloudinaryId = product.CloudinaryId;

            // Database first, image second. 
            // Now the worst case is an orphaned image in Cloudinary, which is harmless.
            var deleted = await _repository.DeleteAsync(id);
            if (deleted == null) return null;

            if (!string.IsNullOrEmpty(cloudinaryId))
            {
                try
                {
                    // PhotoService logs the outcome itself (including "not found").
                    await _photoService.DeletePhotoAsync(cloudinaryId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Product {ProductId} was deleted but its image {PublicId} could not be removed from Cloudinary.",
                        id, cloudinaryId);
                }
            }

            return deleted;
        }

        private static void ValidatePriceAndStock(decimal price, int stockQuantity)
        {
            if (price <= 0)
                throw new BusinessRuleException("Price must be greater than zero.");

            if (stockQuantity < 0)
                throw new BusinessRuleException("Stock quantity cannot be negative.");
        }
    }
}