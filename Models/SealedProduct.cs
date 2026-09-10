using CardShop.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using static api.Enums.ProductEnums;

namespace api.Models
{
    public class SealedProductDetail
    {
        public int Id { get; set; }
        public string? SetName { get; set; }
        public SealedProductType? SealedProductType { get; set; } // e.g. Booster Box, ETB, Bundle
        public string? Language { get; set; }

        public int ProductId { get; set; }
        [ValidateNever]
        public Product? Product { get; set; }
    }
}
