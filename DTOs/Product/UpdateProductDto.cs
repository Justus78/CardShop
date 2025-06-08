using static api.Enums.ProductEnums;
using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Product
{
    public class UpdateProductDto
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
        public string? ImageUrl { get; set; }

        [Required]
        public ProductCategory ProductCategory { get; set; }

        // Optional card-specific fields
        public bool IsFoil { get; set; } = false;
        public CardCondition? CardCondition { get; set; }
        public CardRarity? CardRarity { get; set; }
        public CardType? CardType { get; set; }
        public string? CollectionNumber { get; set; }
        public string? SetName { get; set; }
    } // end dto
} // end namespace
