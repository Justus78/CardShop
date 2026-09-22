using api.DTOs.Cart;
using api.Mappers;
using api.Repositories;
using CardShop.Models;

namespace api.Services
{

    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<IEnumerable<CartItemDto>> GetCartAsync(string userId)
        {
            var items = await _cartRepository.GetCartItemsAsync(userId);
            return items.ToDtoList();
        }

        public async Task<CartItemDto?> AddAsync(string userId, AddCartItemDto dto)
        {
            if (dto.Quantity <= 0) return null;

            var existingItem = await _cartRepository.GetCartItemByProductIdAsync(userId, dto.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                await _cartRepository.SaveChangesAsync();
                return existingItem.ToDto();
            }

            var newItem = new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            };

            await _cartRepository.AddCartItemAsync(newItem);
            await _cartRepository.SaveChangesAsync();
            await _cartRepository.LoadProductAsync(newItem);

            return newItem.ToDto();
        }

        public async Task<CartItemDto?> UpdateAsync(string userId, UpdateCartItemDto dto)
        {
            if (dto.Quantity <= 0) return null;

            var item = await _cartRepository.GetCartItemByIdAsync(userId, dto.Id);
            if (item == null) return null;

            item.Quantity = dto.Quantity;
            await _cartRepository.SaveChangesAsync();

            return item.ToDto();
        }

        public async Task<bool> RemoveAsync(string userId, int cartItemId)
        {
            var item = await _cartRepository.GetCartItemByIdAsync(userId, cartItemId);
            if (item == null) return false;

            _cartRepository.RemoveCartItem(item);
            await _cartRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ClearAsync(string userId)
        {
            var items = await _cartRepository.GetAllForUserAsync(userId);
            _cartRepository.RemoveRange(items);
            return await _cartRepository.SaveChangesAsync();
        }
    }
}
