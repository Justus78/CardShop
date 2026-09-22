using CardShop.Models;

namespace api.Repositories
{
    public interface ICartRepository
    {
        Task<List<CartItem>> GetCartItemsAsync(string userId);
        Task<CartItem?> GetCartItemByProductIdAsync(string userId, int productId);
        Task<CartItem?> GetCartItemByIdAsync(string userId, int cartItemId);
        Task AddCartItemAsync(CartItem item);
        Task LoadProductAsync(CartItem item);
        void RemoveCartItem(CartItem item);
        Task<List<CartItem>> GetAllForUserAsync(string userId); // used for Clear
        void RemoveRange(IEnumerable<CartItem> items);
        Task<bool> SaveChangesAsync();
    }
}