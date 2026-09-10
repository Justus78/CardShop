using CardShop.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using static api.Enums.ProductEnums;

namespace api.Models
{
    public class AccessoryDetail
    {
        public int Id { get; set; }
        public string? Brand { get; set; }
        public AccessoryCategory? AccessoryCategory { get; set; } // e.g. Sleeves, Binder, Playmat
        public string? Dimensions { get; set; }

        public int ProductId { get; set; }
        [ValidateNever]
        public Product? Product { get; set; }
    }
}
