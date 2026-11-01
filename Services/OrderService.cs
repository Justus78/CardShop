using api.DTOs.Order;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.UnitOfWork;
using CardShop.Models;
using Stripe;

namespace api.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IUnitOfWork unitOfWork, IConfiguration config, ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────
        // CREATE ORDER + PAYMENT INTENT
        //
        // PHASE 1 (database only, atomic): reserve stock, price the items from
        //   the DATABASE, and create the Order as Pending. Any failure rolls
        //   back everything.
        // PHASE 2 (Stripe): create the PaymentIntent. If that fails, Phase 1 is
        //   already committed, so we undo it manually.
        // ─────────────────────────────────────────────────────────
        public async Task<CreateOrderResultDto> CreateOrderWithPaymentIntentAsync(CreateOrderDto dto, string userId)
        {
            // ── Input validation, before touching the database or Stripe ──
            if (dto.Items == null || dto.Items.Count == 0)
                throw new BusinessRuleException("An order must contain at least one item.");

            // A zero or negative quantity must never reach the stock logic:
            // "decrement by -5" would INCREASE stock, and the total could go negative.
            if (dto.Items.Any(i => i.Quantity <= 0))
                throw new BusinessRuleException("Item quantities must be greater than zero.");

            // Merge repeated lines for the same product so each product is
            // reserved once, with its combined quantity.
            var lines = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            // ── PHASE 1 ──
            // Runs inside one transaction. May be re-run on a transient SQL
            // failure, so everything it builds is created INSIDE the lambda.
            var order = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var orderItems = new List<OrderItem>();

                foreach (var line in lines)
                {
                    // Atomic check-and-decrement: only succeeds if enough stock
                    // exists at the instant of the UPDATE.
                    var decremented = await _unitOfWork.ProductRepository
                        .TryDecrementStockAsync(line.ProductId, line.Quantity);

                    // Loaded AFTER the decrement attempt. This is the first time this
                    // request loads the product, so StockQuantity is fresh from the DB
                    // (ExecuteUpdate bypasses the change tracker, so a copy loaded
                    // earlier would be stale). It supplies the authoritative PRICE
                    // and the name for error messages.
                    var product = await _unitOfWork.ProductRepository.GetByIdAsync(line.ProductId);

                    if (!decremented || product == null)
                    {
                        // Throwing rolls back any earlier lines that already decremented.
                        // BusinessRuleException is not a transient SQL error, so it is
                        // not retried and surfaces as a 400.
                        throw new BusinessRuleException(product == null
                            ? $"Product {line.ProductId} was not found."
                            : $"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {line.Quantity}");
                    }

                    // If that took stock to 0, remove the product from other users' carts.
                    if (product.StockQuantity == 0)
                    {
                        await _unitOfWork.OrderRepository.RemoveStaleCartItemsAsync(line.ProductId, userId);
                    }

                    orderItems.Add(new OrderItem
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        // Price comes from the database, never from the request.
                        UnitPrice = product.Price
                    });
                }

                var newOrder = new Order
                {
                    UserId = userId,
                    Status = OrderStatus.Pending,
                    PaymentProvider = "Stripe",
                    CreatedDate = DateTime.UtcNow,
                    RecipientName = dto.ShippingInfo.FullName,
                    Street = dto.ShippingInfo.Address,
                    City = dto.ShippingInfo.City,
                    State = dto.ShippingInfo.State,
                    PostalCode = dto.ShippingInfo.PostalCode,
                    Country = dto.ShippingInfo.Country,
                    OrderItems = orderItems,
                    TotalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice)
                };

                await _unitOfWork.OrderRepository.AddAsync(newOrder);
                await _unitOfWork.SaveChangesAsync();

                return newOrder;
            });

            // ── PHASE 2: Stripe. Stock + order are already committed. ──
            try
            {
                StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(order.TotalAmount * 100),
                    Currency = "usd",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                    Shipping = new ChargeShippingOptions
                    {
                        Name = order.RecipientName,
                        Address = new AddressOptions
                        {
                            Line1 = order.Street,
                            City = order.City,
                            State = order.State,
                            PostalCode = order.PostalCode,
                            Country = order.Country
                        }
                    }
                };

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(options);

                order.PaymentIntentId = paymentIntent.Id;
                await _unitOfWork.SaveChangesAsync();

                return new CreateOrderResultDto
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Order = MapToDto(order)
                };
            }
            catch (StripeException)
            {
                // Compensating action: Phase 1 already committed, so restore the
                // stock and cancel the order by hand.
                foreach (var item in order.OrderItems)
                {
                    await _unitOfWork.ProductRepository.RestoreStockAsync(item.ProductId, item.Quantity);
                }

                order.Status = OrderStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync();

                throw;
            }
        }

        // ─────────────────────────────────────────────────────────
        // MARK ORDER PAID (called by the Stripe webhook, and by the sweep)
        //
        // Only a PENDING order can become Paid. Before this guard, any order
        // was flipped to Paid, including one the sweep had already cancelled
        // and whose stock was already released.
        // ─────────────────────────────────────────────────────────
        public async Task MarkOrderPaidAsync(string paymentIntentId)
        {
            var order = await _unitOfWork.OrderRepository.GetByPaymentIntentIdAsync(paymentIntentId);
            if (order == null) return;

            if (order.Status != OrderStatus.Pending)
            {
                // Paid / Shipped: a duplicate webhook delivery, nothing to do.
                // Cancelled / Failed: the customer was charged but the order was
                // already released. We do NOT flip it to Paid, because there is no
                // stock behind it. A human needs to refund it.
                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Failed)
                {
                    _logger.LogError(
                        "Payment succeeded for PaymentIntent {PaymentIntentId}, but order {OrderId} is already {Status} " +
                        "and its stock was released. Manual review / refund required.",
                        paymentIntentId, order.Id, order.Status);
                }
                return;
            }

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────
        // EXPIRE STALE ORDERS (called by OrderExpirationBackgroundService)
        //
        // For each Pending order older than `olderThan`, Stripe is the source of
        // truth about whether it can still be paid. Stock is released ONLY once
        // Stripe confirms the PaymentIntent can no longer succeed.
        //
        // Returns how many orders were cancelled and released.
        // ─────────────────────────────────────────────────────────
        public async Task<int> ExpireStaleOrdersAsync(TimeSpan olderThan)
        {
            // The key is a static setting in the Stripe library. The sweep may run
            // before any order has been created since startup, so set it here too.
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var cutoff = DateTime.UtcNow - olderThan;
            var staleOrders = await _unitOfWork.OrderRepository.GetStalePendingOrdersAsync(cutoff);

            var releasedCount = 0;
            var paymentIntentService = new PaymentIntentService();

            foreach (var order in staleOrders)
            {
                // Copy plain values now; the entity may be detached if a transaction retries.
                var orderId = order.Id;
                var paymentIntentId = order.PaymentIntentId;
                var items = order.OrderItems.Select(i => (i.ProductId, i.Quantity)).ToList();

                // One bad order must not stop the rest of the sweep.
                try
                {
                    if (!string.IsNullOrEmpty(paymentIntentId))
                    {
                        var intent = await paymentIntentService.GetAsync(paymentIntentId);

                        switch (intent.Status)
                        {
                            case "succeeded":
                                // The customer DID pay; the webhook was delayed or missed.
                                // Complete the order instead of cancelling it.
                                _logger.LogWarning(
                                    "Stale order {OrderId} was already paid in Stripe ({PaymentIntentId}); marking it Paid.",
                                    orderId, paymentIntentId);
                                await MarkOrderPaidAsync(paymentIntentId);
                                continue;

                            case "processing":
                            case "requires_capture":
                                // A payment is in flight. Leave it for a later sweep.
                                continue;

                            case "canceled":
                                // Already dead in Stripe; safe to release below.
                                break;

                            default:
                                // Still open (requires_payment_method, requires_action, ...).
                                // Cancel it in Stripe FIRST so it can't be paid after we
                                // release the stock.
                                try
                                {
                                    await paymentIntentService.CancelAsync(paymentIntentId,
                                        new PaymentIntentCancelOptions { CancellationReason = "abandoned" });
                                }
                                catch (StripeException ex)
                                {
                                    // Most likely the customer paid between our check and
                                    // the cancel. Leave the order Pending: the next sweep
                                    // will see "succeeded" and complete it.
                                    _logger.LogWarning(ex,
                                        "Could not cancel PaymentIntent {PaymentIntentId} for order {OrderId}; leaving it for the next sweep.",
                                        paymentIntentId, orderId);
                                    continue;
                                }
                                break;
                        }
                    }
                    // (No PaymentIntentId means Stripe was never reached for this order,
                    // so there is nothing to cancel there.)

                    if (await ReleaseOrderAsync(orderId, items))
                    {
                        releasedCount++;
                    }
                }
                catch (Exception ex)
                {
                    // Includes Stripe being unreachable. The order stays Pending and is
                    // retried on the next sweep; we never cancel what we couldn't verify.
                    _logger.LogError(ex, "Failed to expire order {OrderId}; it will be retried on the next sweep.", orderId);
                }
            }

            return releasedCount;
        }

        // Cancels one Pending order and restores its stock, as a single transaction.
        // The status change is a conditional UPDATE ("only if still Pending"), so it
        // reads the real database value, not a possibly stale in-memory copy. If the
        // order is no longer Pending (e.g. it was just paid), nothing is restored.
        private async Task<bool> ReleaseOrderAsync(int orderId, List<(int ProductId, int Quantity)> items)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var cancelled = await _unitOfWork.OrderRepository.TryCancelPendingOrderAsync(orderId);
                if (!cancelled) return false;

                foreach (var (productId, quantity) in items)
                {
                    await _unitOfWork.ProductRepository.RestoreStockAsync(productId, quantity);
                }

                return true;
            });
        }

        public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
        {
            var orders = await _unitOfWork.OrderRepository.GetForUserAsync(userId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId, userId);
            return order == null ? null : MapToDto(order);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            order.Status = newStatus;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static OrderDto MapToDto(Order order)
        {
            // Pre-existing caveat: order.User may not be loaded on a freshly
            // created order, so Username can come back null there.
            return new OrderDto
            {
                Id = order.Id,
                Username = order.User?.UserName,
                PaymentIntentId = order.PaymentIntentId,
                Status = order.Status,
                CreatedAt = order.CreatedDate,
                PaidAt = order.PaidAt,
                RecipientName = order.RecipientName,
                Street = order.Street,
                City = order.City,
                State = order.State,
                PostalCode = order.PostalCode,
                Country = order.Country,
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
    }
}