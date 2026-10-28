using api.Interfaces;
using api.Repositories;

namespace api.UnitOfWork
{
    public interface IUnitOfWork
    {
        ICartRepository CartRepository { get; }
        IOrderRepository OrderRepository { get; }
        IProductRepository ProductRepository { get; }

        // The single save point for operations spanning multiple repos.
        // One SaveChangesAsync() call is always atomic on its own.
        Task<int> SaveChangesAsync();

        // ─────────────────────────────────────────────────────────
        // Runs `operation` inside a database transaction that is safe to use
        // with EnableRetryOnFailure().
        //
        // Replaces the old BeginTransactionAsync/CommitTransactionAsync/
        // RollbackTransactionAsync trio: those open a transaction "by hand,"
        // which EF's retrying execution strategy refuses to allow (it throws
        // "does not support user-initiated transactions").
        //
        // How it behaves:
        //  - Everything inside `operation` commits together, or not at all.
        //  - If `operation` throws, the transaction rolls back and the
        //    exception is rethrown to the caller.
        //  - If a TRANSIENT SQL failure occurs (dropped connection, deadlock,
        //    etc.), EF may run `operation` AGAIN from the top. So the code
        //    inside must be safe to re-run — build new objects inside the
        //    lambda rather than reusing ones created outside it, and don't
        //    do non-database side effects (like calling Stripe) in here.
        // ─────────────────────────────────────────────────────────
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}