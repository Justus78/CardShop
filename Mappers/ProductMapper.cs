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

            // Only create card detail if category is Card and CardDetail DTO is provided
            if (dto.ProductCategory == ProductCategory.Card && dto.CardDetails != null)
            {
                product.CardDetails = new CardDetail
                {
                    IsFoil = dto.CardDetails.IsFoil,
                    CardCondition = dto.CardDetails.CardCondition,
                    CardRarity = dto.CardDetails.CardRarity,
                    CardType = dto.CardDetails.CardType,
                    CollectionNumber = dto.CardDetails.CollectionNumber,
                    SetName = dto.CardDetails.SetName,
                };
            }

            return product;
        }
    }
}
