using api.Interfaces;
using api.Repositories;

namespace api.UnitOfWork
{
    // ─────────────────────────────────────────────────────────────
    // What problem this solves:
    // Right now, CartRepository, OrderRepository, and ProductRepository
    // each have their OWN SaveChangesAsync() — but they all secretly share
    // the SAME ApplicationDbContext (because it's registered as Scoped,
    // meaning one instance per HTTP request). That means calling
    // _cartRepository.SaveChangesAsync() doesn't just save cart changes —
    // it saves EVERYTHING EF Core is currently tracking across all three
    // repos, whether you meant to or not.
    //
    // That's harmless today because nothing touches two repos in one
    // request. It becomes a real problem the moment you build checkout:
    //   1. Create the Order        (OrderRepository)
    //   2. Decrement Product stock (ProductRepository)
    //   3. Clear the user's Cart   (CartRepository)
    // If step 2 fails after step 1's SaveChangesAsync() already ran,
    // you've got an Order in the DB for stock that was never actually
    // reserved — a half-finished operation.
    //
    // IUnitOfWork fixes this by being the ONE place SaveChangesAsync lives
    // for multi-repo operations. A service doing checkout injects
    // IUnitOfWork (not three separate repos), does all its work, and
    // calls _unitOfWork.SaveChangesAsync() exactly once at the end —
    // or wraps the whole thing in a transaction if it needs to call
    // out to something like a payment API in between steps.
    // ─────────────────────────────────────────────────────────────
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Exposes your existing repos as properties, so a service that needs
        // more than one of them only takes a single IUnitOfWork dependency
        // instead of three separate constructor parameters.
        ICartRepository CartRepository { get; }
        IOrderRepository OrderRepository { get; }
        IProductRepository ProductRepository { get; }

        // The ONE save call for any operation spanning multiple repos.
        // Returns the number of rows affected, same as EF's own SaveChangesAsync.
        Task<int> SaveChangesAsync();

        // For the rarer case where you need an explicit DB transaction —
        // e.g. you're calling an external API (a payment provider) BETWEEN
        // two SaveChangesAsync calls, and need to roll back the first save
        // if the second step fails. Most operations won't need this; a single
        // SaveChangesAsync() call is already atomic on its own since EF wraps
        // it in an implicit transaction automatically.
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}