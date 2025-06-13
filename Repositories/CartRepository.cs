using Microsoft.EntityFrameworkCore;
using CardShop.Data;
using CardShop.Models;
using api.DTOs.Cart;
using api.Mappers;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CartItemDto>> GetCartAsync(string userId)
    {
        var items = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        return items.ToDtoList();
    }

    public async Task<CartItemDto?> AddAsync(string userId, AddCartItemDto dto)
    {
        // Check if item already exists in cart
        var existingItem = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            await _context.SaveChangesAsync();
            return existingItem.ToDto();
        }

        // Add new item
        var newItem = new CartItem
        {
            UserId = userId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity
        };

        _context.CartItems.Add(newItem);
        await _context.SaveChangesAsync();

        // Load product for mapping
        await _context.Entry(newItem).Reference(ci => ci.Product).LoadAsync();

        return newItem.ToDto();
    }

    public async Task<CartItemDto?> UpdateAsync(string userId, UpdateCartItemDto dto)
    {
        var item = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.Id == dto.Id);

        if (item == null) return null;

        item.Quantity = dto.Quantity;
        await _context.SaveChangesAsync();

        return item.ToDto();
    }

    public async Task<bool> RemoveAsync(string userId, int cartItemId)
    {
        var item = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.Id == cartItemId);

        if (item == null) return false;

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task ClearAsync(string userId)
    {
        var items = _context.CartItems.Where(ci => ci.UserId == userId);
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
