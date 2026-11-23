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
        // The most unpaid (Pending) orders one user may hold at once. Every Pending order
        // reserves stock until it is paid or expires (about 30 minutes), so without a limit
        // one account could tie up the whole inventory by starting checkouts it never pays
        // for. It is a soft limit: two requests at the very same instant could both pass
        // the check, which the order-creation rate limit keeps small.
        private const int MaxPendingOrdersPerUser = 3;

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

            // ── Abuse protection: limit how many unpaid orders one user can hold ──
            var pendingCount = await _unitOfWork.OrderRepository.CountPendingOrdersAsync(userId);
            if (pendingCount >= MaxPendingOrdersPerUser)
            {
                _logger.LogWarning(
                    "User {UserId} was refused a new order: they already hold {PendingCount} unpaid orders.",
                    userId, pendingCount);

                // (If the expiration window in OrderExpirationBackgroundService changes
                // from 30 minutes, update the number in this message.)
                throw new BusinessRuleException(
                    $"You already have {pendingCount} unpaid orders waiting for payment. " +
                    "Please complete one, or try again later. Unpaid orders are released automatically after about 30 minutes.");
            }

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

            _logger.LogInformation(
                "Order {OrderId} created for user {UserId}: {LineCount} line(s), total {TotalAmount}. Stock reserved.",
                order.Id, userId, order.OrderItems.Count, order.TotalAmount);

            // ── PHASE 2: Stripe. Stock + order are already committed. ──
            try
            {
                StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

                var options = new PaymentIntentCreateOptions
                {
                    // Rounded rather than truncated, so a value like 19.995 can't lose a cent.
                    Amount = (long)Math.Round(order.TotalAmount * 100, MidpointRounding.AwayFromZero),
                    Currency = "usd",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },

                    // Lets you find the order from the Stripe dashboard. If a payment ever
                    // arrives with no matching order, this says which order it was for.
                    Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() },

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

                _logger.LogInformation("PaymentIntent {PaymentIntentId} created for order {OrderId}.",
                    paymentIntent.Id, order.Id);

                return new CreateOrderResultDto
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Order = MapToDto(order)
                };
            }
            catch (StripeException ex)
            {
                // Before this, the Stripe failure was only visible as a generic 500. This
                // line says which order was affected and that the stock was released.
                _logger.LogError(ex,
                    "Stripe PaymentIntent creation failed for order {OrderId}; releasing the reserved stock and cancelling the order.",
                    order.Id);

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
            if (order == null)
            {
                // The customer was charged but nothing in our database matches. Without
                // this line the webhook would answer 200 and nobody would ever know.
                _logger.LogWarning(
                    "Payment succeeded for PaymentIntent {PaymentIntentId} but no matching order exists. Manual review / refund required.",
                    paymentIntentId);
                return;
            }

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

            // Stock was already reserved when the order was created, so nothing to subtract
            // here. But if this purchase took a product's stock to zero, the sale is now
            // permanent, so remove that product from OTHER users' carts.
            //
            // This used to run at reservation time. That emptied other people's carts even
            // if the order was later abandoned and the stock came back, and let anyone
            // clear a product out of every cart just by reserving the last units unpaid.
            foreach (var item in order.OrderItems)
            {
                if (item.Product != null && item.Product.StockQuantity == 0)
                {
                    await _unitOfWork.OrderRepository.RemoveStaleCartItemsAsync(item.ProductId, order.UserId!);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} marked Paid (PaymentIntent {PaymentIntentId}).",
                order.Id, paymentIntentId);
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
                        _logger.LogInformation(
                            "Released abandoned order {OrderId}: stock restored and order cancelled.", orderId);
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

        // Which status changes an ADMIN may make by hand. Deliberately small.
        //
        // Setting Cancelled/Failed here would NOT give back the stock the order reserved
        // (only the expiration sweep and the Stripe-failure path release stock), and
        // moving a cancelled order back to Pending/Paid would sell stock that was already
        // released. Pending orders belong to Stripe and the sweep, so an admin can't mark
        // one Paid either. Cancelling a PAID order needs a refund flow, which is
        // intentionally not offered yet.
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AdminAllowedTransitions = new()
        {
            [OrderStatus.Paid] = new[] { OrderStatus.Shipped },
            [OrderStatus.Shipped] = new[] { OrderStatus.Paid }, // undo a mistaken "shipped"
        };

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            if (order.Status == newStatus) return true; // nothing to change

            if (!AdminAllowedTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(newStatus))
            {
                // BusinessRuleException becomes a 400 through the global exception middleware.
                throw new BusinessRuleException(
                    $"An order that is {order.Status} cannot be changed to {newStatus}.");
            }

            var oldStatus = order.Status;
            order.Status = newStatus;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}.",
                orderId, oldStatus, newStatus);

            return true;
        }

        private static OrderDto MapToDto(Order order)
        {
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