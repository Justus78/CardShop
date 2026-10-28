using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using CardShop.Models;
using Microsoft.EntityFrameworkCore;
using static api.Enums.ProductEnums;

namespace api.Repositories 
{ 
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync() => await _context.Products.CountAsync();

        public async Task<Product> AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> DeleteAsync(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return null;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<PagedResult<Product>> GetAllAsync(ProductQueryObject queryObject)
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.CardDetails)
                .Include(p => p.SealedProductDetails)
                .Include(p => p.AccessoryDetails);

            /* ---------- Filtering ---------- */

            if (queryObject.Category.HasValue)
                query = query.Where(p => p.ProductCategory == queryObject.Category.Value);

            if (queryObject.MinPrice.HasValue)
                query = query.Where(p => p.Price >= queryObject.MinPrice.Value);

            if (queryObject.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= queryObject.MaxPrice.Value);

            if (queryObject.InStockOnly == true)
                query = query.Where(p => p.StockQuantity > 0);

            if (!string.IsNullOrWhiteSpace(queryObject.SearchTerm))
                query = query.Where(p =>
                    p.Name!.Contains(queryObject.SearchTerm) ||
                    p.Description!.Contains(queryObject.SearchTerm));

            // Card-specific filters — EF translates these into a join against CardDetails automatically
            if (queryObject.Rarity.HasValue)
                query = query.Where(p => p.CardDetails != null && p.CardDetails.CardRarity == queryObject.Rarity.Value);

            if (queryObject.IsFoil.HasValue)
                query = query.Where(p => p.CardDetails != null && p.CardDetails.IsFoil == queryObject.IsFoil.Value);

            /* ---------- Sorting ---------- */

            query = queryObject.SortBy?.ToLower() switch
            {
                "name" => queryObject.Ascending
                    ? query.OrderBy(p => p.Name)
                    : query.OrderByDescending(p => p.Name),

                "price" => queryObject.Ascending
                    ? query.OrderBy(p => p.Price)
                    : query.OrderByDescending(p => p.Price),

                "category" => queryObject.Ascending
                    ? query.OrderBy(p => p.ProductCategory)
                    : query.OrderByDescending(p => p.ProductCategory),

                _ => queryObject.Ascending
                    ? query.OrderBy(p => p.Id)
                    : query.OrderByDescending(p => p.Id)
            };

            /* ---------- Pagination ---------- */

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((queryObject.Page - 1) * queryObject.PageSize)
                .Take(queryObject.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                Page = queryObject.Page,
                PageSize = queryObject.PageSize
            };
        } // end get all

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.CardDetails)
                .Include(p => p.SealedProductDetails)
                .Include(p => p.AccessoryDetails)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

        // Why this isn't just "read stock, check in C#, write stock":
        // That pattern has a race condition — two requests can both read StockQuantity
        // as 1 before either one writes, both pass the "is there enough?" check, and
        // you end up at -1. ExecuteUpdateAsync instead sends ONE SQL statement where
        // the check and the write happen together, atomically, at the database level:
        //
        //   UPDATE Products SET StockQuantity = StockQuantity - @quantity
        //   WHERE Id = @productId AND StockQuantity >= @quantity
        //
        // If two requests race for the last unit, the database guarantees only one
        // of them can actually apply — the other gets 0 rows affected, which we
        // read as "not enough stock," with nothing ever going negative.
        //
        // Trade-off worth knowing: ExecuteUpdateAsync runs IMMEDIATELY against the
        // database — it does NOT wait for SaveChangesAsync and is not tracked by
        // the ChangeTracker the way _context.Products.Update(...) would be. That
        // means if you call this multiple times in a row (once per item in an
        // order) and a later call fails, the earlier ones have ALREADY been
        // committed to the database — you can't just "not call SaveChanges" to
        // undo them. This is exactly why OrderService wraps multiple calls to this
        // method in an explicit transaction (via IUnitOfWork.BeginTransactionAsync),
        // so a failure partway through can be properly rolled back.
        public async Task<bool> TryDecrementStockAsync(int productId, int quantity)
        {
            var rowsAffected = await _context.Products
                .Where(p => p.Id == productId && p.StockQuantity >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity));

            // ExecuteUpdateAsync returns how many rows matched the WHERE clause and
            // were updated. 1 = success. 0 = either the product doesn't exist, or
            // StockQuantity < quantity (not enough stock) — both read as "failed."
            return rowsAffected == 1;
        }

        // Restoring stock doesn't have the same race-condition risk as decrementing
        // (nothing else is competing to restore the same stock at the same instant),
        // so no conditional WHERE clause is needed here — just add it back.
        public async Task RestoreStockAsync(int productId, int quantity)
        {
            await _context.Products
                .Where(p => p.Id == productId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity));
        }
    }
}


