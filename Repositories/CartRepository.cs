using Microsoft.EntityFrameworkCore;
using CardShop.Data;
using CardShop.Models;

namespace api.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(string userId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .ToListAsync();
        }

        public async Task<CartItem?> GetCartItemByProductIdAsync(string userId, int productId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);
        }

        public async Task<CartItem?> GetCartItemByIdAsync(string userId, int cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.Id == cartItemId);
        }

        public async Task AddCartItemAsync(CartItem item)
        {
            await _context.CartItems.AddAsync(item);
        }

        public async Task LoadProductAsync(CartItem item)
        {
            await _context.Entry(item).Reference(ci => ci.Product).LoadAsync();
        }

        public void RemoveCartItem(CartItem item)
        {
            _context.CartItems.Remove(item);
        }

        public async Task<List<CartItem>> GetAllForUserAsync(string userId)
        {
            return await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();
        }

        public void RemoveRange(IEnumerable<CartItem> items)
        {
            _context.CartItems.RemoveRange(items);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}