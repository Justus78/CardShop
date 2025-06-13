using System.Collections.Generic;
using System.Threading.Tasks;
using api.DTOs.Cart;
using CardShop.Models;

public interface ICartService
{
    Task<IEnumerable<CartItemDto>> GetCartAsync(string userId);
    Task<CartItemDto?> AddAsync(string userId, AddCartItemDto dto);
    Task<CartItemDto?> UpdateAsync(string userId, UpdateCartItemDto dto);
    Task<bool> RemoveAsync(string userId, int cartItemId);
    Task ClearAsync(string userId);
}
