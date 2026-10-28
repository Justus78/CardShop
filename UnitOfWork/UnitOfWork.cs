using api.Interfaces;
using api.Repositories;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;

namespace api.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        // Repos are created lazily, and all share the same _context — which is
        // what makes a single SaveChangesAsync() commit changes across all of them.
        private ICartRepository? _cartRepository;
        private IOrderRepository? _orderRepository;
        private IProductRepository? _productRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public ICartRepository CartRepository => _cartRepository ??= new CartRepository(_context);
        public IOrderRepository OrderRepository => _orderRepository ??= new OrderRepository(_context);
        public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            // Your DbContext is configured with EnableRetryOnFailure(), so EF wraps
            // database work in a "retrying execution strategy." That strategy needs
            // to own the whole unit of work so it can replay it from the start if a
            // transient failure happens. CreateExecutionStrategy() gives us that
            // handle, and everything inside ExecuteAsync is treated as one
            // retriable block — including opening and committing the transaction.
            var strategy = _context.Database.CreateExecutionStrategy();

            var attempt = 0;

            return await strategy.ExecuteAsync(async () =>
            {
                // If this is a RETRY (attempt > 0), the previous attempt may have
                // left half-finished entities in EF's change tracker (e.g. an Order
                // we'd Add()ed but never committed). Clear them so the retry starts
                // from a clean slate and doesn't insert duplicates.
                if (attempt++ > 0)
                {
                    _context.ChangeTracker.Clear();
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    // Undoes everything the operation did on this connection —
                    // including ExecuteUpdateAsync stock decrements, which run
                    // immediately and aren't covered by the change tracker.
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}