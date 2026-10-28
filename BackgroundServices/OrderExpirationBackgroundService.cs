using api.Interfaces;

namespace api.BackgroundServices
{
    // ─────────────────────────────────────────────────────────────
    // What a BackgroundService is:
    // ASP.NET Core can run long-lived background tasks alongside your normal
    // request handling. BackgroundService is a base class for exactly this —
    // you override ExecuteAsync, and the host starts it when the app starts
    // and stops it (via the cancellation token) when the app shuts down.
    //
    // IUnitOfWork — and the DbContext underneath it — is SCOPED (meant to live
    // for one request, then get disposed). You can't just inject IUnitOfWork
    // into this class's constructor the way a controller does; you'd get one
    // instance that outlives a database connection meant to be short-lived,
    // which causes stale-data and threading problems.
    //
    // The fix: inject IServiceScopeFactory instead, and manually create a new,
    // short-lived scope EVERY time the timer fires — getting a fresh
    // IUnitOfWork (and fresh DbContext) for each sweep, then disposing it
    // right after. 
    // ─────────────────────────────────────────────────────────────
    public class OrderExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderExpirationBackgroundService> _logger;

        // How often the sweep runs. Doesn't need to match the expiration
        // window — running every 5 minutes just means an abandoned order
        // gets caught somewhere between 30 and 35 minutes after creation,
        // which is fine for this purpose.
        private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

        // How old a Pending order has to be before we consider it abandoned.
        // This is the "30 minutes" you decided on.
        private static readonly TimeSpan ExpirationWindow = TimeSpan.FromMinutes(30);

        public OrderExpirationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderExpirationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ExecuteAsync is called once, automatically, when the app starts.
        // The while loop is what makes this a RECURRING job rather than a
        // one-shot startup task — it keeps running until the app shuts down
        // (signaled via stoppingToken).
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(SweepInterval);

            // WaitForNextTickAsync returns false once stoppingToken is cancelled
            // (i.e. the app is shutting down), which ends the loop cleanly.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SweepExpiredOrdersAsync();
                }
                catch (Exception ex)
                {
                    // IMPORTANT: catch here, don't let an exception escape this loop.
                    // If a single sweep throws and this isn't caught, the ENTIRE
                    // background service stops running permanently — no more sweeps,
                    // ever, until the app restarts. Logging and continuing means one
                    // bad sweep (e.g. a transient DB hiccup) doesn't silently kill
                    // order expiration for the rest of the app's lifetime.
                    _logger.LogError(ex, "Error while sweeping for expired orders.");
                }
            }
        }

        private async Task SweepExpiredOrdersAsync()
        {
            // Create a fresh DI scope for this one sweep — gives us a short-lived
            // IUnitOfWork/DbContext, used only for this sweep, then disposed when
            // the `using` block ends. This is what makes it safe to use scoped
            // services from a singleton background service.
            using var scope = _scopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var expiredCount = await orderService.ExpireStaleOrdersAsync(ExpirationWindow);

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "Order expiration sweep: expired {Count} stale pending order(s) older than {Window}.",
                    expiredCount, ExpirationWindow);
            }
            // No log line when expiredCount == 0 — the common case, every 5 minutes,
            // doesn't need to clutter your logs.
        }
    }
}