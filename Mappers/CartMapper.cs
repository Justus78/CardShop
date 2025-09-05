using api.DTOs.Cart;
using CardShop.Models;

namespace api.Mappers
{
    public static class CartMapper
    {
        public static CartItemDto ToDto(this CartItem item)
        {
            if (item == null) return null;

            return new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                ImageUrl = item.Product?.ImageUrl ?? string.Empty,
                Price = item.Product?.Price ?? 0m,
                Quantity = item.Quantity,
                Set = item.Product?.SetName ?? string.Empty,
            };
        }

        public static IEnumerable<CartItemDto> ToDtoList(this IEnumerable<CartItem> items)
        {
            return items?.Select(item => item.ToDto()) ?? Enumerable.Empty<CartItemDto>();
        }
    }
}
