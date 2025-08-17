using CardShop.Models;
using api.DTOs.Product;


namespace api.Mappers
{
    public static class ProductMapper
    {
        public static Product ToProduct(this CreateProductDto dto)
        {
            return new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ProductCategory = dto.ProductCategory,
                IsFoil = dto.IsFoil,
                CardCondition = dto.CardCondition,
                CardRarity = dto.CardRarity,
                CardType = dto.CardType,
                CollectionNumber = dto.CollectionNumber,
                SetName = dto.SetName,
                BestSeller = dto.BestSeller,
            };
        }
    }
}
