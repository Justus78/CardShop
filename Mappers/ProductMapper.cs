using api.DTOs.Product;
using api.Models;
using CardShop.Models;
using static api.Enums.ProductEnums;


namespace api.Mappers
{
    public static class ProductMapper
    {
        public static Product ToProduct(this CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ProductCategory = dto.ProductCategory,
                BestSeller = dto.BestSeller,
            };

            if (dto.ProductCategory == ProductCategory.Card && dto.CardDetails != null)
            {
                product.CardDetails = new CardDetail
                {
                    IsFoil = dto.CardDetails.IsFoil,
                    FoilType = dto.CardDetails.FoilType,
                    CardCondition = dto.CardDetails.CardCondition,
                    CardRarity = dto.CardDetails.CardRarity,
                    CardType = dto.CardDetails.CardType,
                    CollectionNumber = dto.CardDetails.CollectionNumber,
                    SetName = dto.CardDetails.SetName,
                };
            }

            if (dto.ProductCategory == ProductCategory.Sealed && dto.SealedProductDetails != null)
            {
                product.SealedProductDetails = new SealedProductDetail
                {
                    SetName = dto.SealedProductDetails.SetName,
                    SealedProductType = dto.SealedProductDetails.SealedProductType,
                    Language = dto.SealedProductDetails.Language,
                };
            }

            if (dto.ProductCategory == ProductCategory.Accessory && dto.AccessoryDetails != null)
            {
                product.AccessoryDetails = new AccessoryDetail
                {
                    Brand = dto.AccessoryDetails.Brand,
                    AccessoryCategory = dto.AccessoryDetails.AccessoryCategory,
                    Dimensions = dto.AccessoryDetails.Dimensions,
                };
            }

            return product;
        }
    }
}