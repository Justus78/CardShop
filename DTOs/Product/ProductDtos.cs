using api.Models;
using System.ComponentModel.DataAnnotations;
using static api.Enums.ProductEnums;

namespace api.DTOs.Product
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(255, ErrorMessage = "Name is too long")]
        public string Name { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int StockQuantity { get; set; }
        public string? ProductImage { get; set; }
        public bool? BestSeller { get; set; } = false;

        [Required]
        public ProductCategory ProductCategory { get; set; }

        public CardDetailsDto? CardDetails { get; set; }
        public SealedProductDetailsDto? SealedProductDetails { get; set; }
        public AccessoryDetailsDto? AccessoryDetails { get; set; }
    }

    public class CardDetailsDto
    {
        public bool IsFoil { get; set; } = false;
        public FoilType? FoilType { get; set; } = null;
        public CardCondition? CardCondition { get; set; }
        public CardRarity? CardRarity { get; set; }
        public CardType? CardType { get; set; }
        public string? CollectionNumber { get; set; }
        public string? SetName { get; set; }
    }

    public class SealedProductDetailsDto
    {
        public string? SetName { get; set; }
        public SealedProductType? SealedProductType { get; set; }
        public string? Language { get; set; }
    }

    public class AccessoryDetailsDto
    {
        public string? Brand { get; set; }
        public AccessoryCategory? AccessoryCategory { get; set; }
        public string? Dimensions { get; set; }
    }

    public class UpdateCardDetailDto
    {
        public bool IsFoil { get; set; }
        public FoilType? FoilType { get; set; }
        public CardCondition? CardCondition { get; set; }
        public CardRarity? CardRarity { get; set; }
        public CardType? CardType { get; set; }
        public string? CollectionNumber { get; set; }
        public string? SetName { get; set; }
    }

    public class UpdateSealedProductDetailDto
    {
        public string? SetName { get; set; }
        public SealedProductType? SealedProductType { get; set; }
        public string? Language { get; set; }
    }

    public class UpdateAccessoryDetailDto
    {
        public string? Brand { get; set; }
        public AccessoryCategory? AccessoryCategory { get; set; }
        public string? Dimensions { get; set; }
    }

    public class UpdateProductDto
    {
        [Required]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public bool? BestSeller { get; set; }

        public UpdateCardDetailDto? CardDetail { get; set; }
        public UpdateSealedProductDetailDto? SealedProductDetail { get; set; }
        public UpdateAccessoryDetailDto? AccessoryDetail { get; set; }
    }

    public class ProductQueryObject
    {
        // Filtering
        public ProductCategory? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; }
        public string? SearchTerm { get; set; } // matches Name/Description

        // Card-specific filters (only meaningful when Category == Card, but harmless otherwise)
        public CardRarity? Rarity { get; set; }
        public bool? IsFoil { get; set; }

        // Sorting
        public string? SortBy { get; set; } // "name", "price", "category"
        public bool Ascending { get; set; } = true;

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}