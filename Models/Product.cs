using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using static api.Enums.ProductEnums;

namespace CardShop.Models
{
    // Models/Product.cs
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public ProductCategory ProductCategory { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; } // url for cloudinary image location
        public string? CloudinaryId { get; set; } // for the cloudinary result public Id to located photos to delete

        // properites for singles cards
        public bool IsFoil { get; set; } = false;        
        [Required]
        public CardCondition? CardCondition { get; set; }
        public CardRarity? CardRarity { get; set; }
        public CardType? CardType { get; set; }
        public string? CollectionNumber { get; set; }
        public string? SetName { get; set; }


        // navigation properties for many to many relationships
        [ValidateNever]
        public ICollection<CartItem>? CartItems { get; set; }
        [ValidateNever]
        public ICollection<OrderItem>? OrderItems { get; set; }
    }

}
