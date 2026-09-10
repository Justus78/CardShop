using CardShop.Models;
using static api.Enums.ProductEnums;

namespace api.Services
{
    public class ProductValidationException : Exception
    {
        public ProductValidationException(string message) : base(message) { }
    }

    public static class ProductValidator
    {
        public static void ValidateProductDetails(Product product)
        {
            var detailCount = new[]
            {
            product.CardDetails != null,
            product.SealedProductDetails != null,
            product.AccessoryDetails != null
        }.Count(present => present);

            if (detailCount == 0)
                throw new ProductValidationException(
                    $"Product '{product.Name}' must have exactly one detail type set, but none was provided.");

            if (detailCount > 1)
                throw new ProductValidationException(
                    $"Product '{product.Name}' must have exactly one detail type set, but multiple were provided.");

            var expected = product.ProductCategory switch
            {
                ProductCategory.Card => product.CardDetails != null,
                ProductCategory.Sealed => product.SealedProductDetails != null,
                ProductCategory.Accessory => product.AccessoryDetails != null,
                _ => throw new ProductValidationException(
                    $"Unknown or unhandled ProductCategory: {product.ProductCategory}")
            };

            if (!expected)
                throw new ProductValidationException(
                    $"Product '{product.Name}' has ProductCategory '{product.ProductCategory}' " +
                    $"but the corresponding detail object was not provided.");
        }
    }
}
