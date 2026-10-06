using api.Interfaces;
using api.Repositories;
using CardShop.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace api.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        // Holds the active transaction, if BeginTransactionAsync was called.
        // Null means "no explicit transaction in progress" — the normal case.
        private IDbContextTransaction? _transaction;

        // Repos are created lazily — only instantiated the first time
        // something actually accesses them. If a service only ever touches
        // CartRepository, there's no reason to also construct an unused
        // OrderRepository and ProductRepository for that request.
        private ICartRepository? _cartRepository;
        private IOrderRepository? _orderRepository;
        private IProductRepository? _productRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // The "??=" below means: if the backing field is null, create it now
        // and store it; if it already exists, just return the existing instance.
        // This guarantees all repos accessed through THIS UnitOfWork instance
        // share the same ApplicationDbContext — which is what makes the atomic
        // SaveChangesAsync() below actually work.
        public ICartRepository CartRepository => _cartRepository ??= new CartRepository(_context);
        public IOrderRepository OrderRepository => _orderRepository ??= new OrderRepository(_context);
        public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);

        // The single save point for any operation spanning multiple repos.
        // Because CartRepository, OrderRepository, and ProductRepository above
        // all share _context, this one call commits every change EF is tracking
        // across all of them — in one transaction, automatically, even without
        // an explicit BeginTransactionAsync call. That's standard EF Core
        // behavior: a single SaveChangesAsync() is always atomic.
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // ── Explicit transaction support, for the less common case ──
        // Use this when you need to do something NON-database between two
        // separate SaveChangesAsync calls — e.g. call a payment provider's API
        // after saving a pending Order, then save again once payment confirms.
        // Without an explicit transaction, the first SaveChangesAsync already
        // committed — you couldn't undo it if the payment call failed.

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null) return;

            try
            {
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null) return;

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Cleans up the transaction if the UnitOfWork is disposed while one
        // is still open (e.g. an exception skipped the Commit/Rollback call).
        // The DbContext itself is NOT disposed here — it's registered as
        // Scoped in DI, so the container owns its lifetime, not this class.
        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
        }
    }
}