using CardShop.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Runtime.InteropServices;
using static api.Enums.ProductEnums;

namespace api.Models
{
    public class CardDetail
    {
        // properites for singles cards
        public int Id { get; set; }
        public bool IsFoil { get; set; } = false;
        public FoilType FoilType { get; set; } = FoilType.NonFoil;

        public CardCondition? CardCondition { get; set; }
        public CardRarity? CardRarity { get; set; }
        public CardType? CardType { get; set; }
        public string? CollectionNumber { get; set; }
        public string? SetName { get; set; }

        // foreign key for product
        public int ProductId { get; set; }
        [ValidateNever]
        public Product? Product {  get; set; }
    }
}
