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

        public OrderService(IUnitOfWork unitOfWork, IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _config = config;
        }

        // ─────────────────────────────────────────────────────────
        // CREATE ORDER + PAYMENT INTENT
        //
        // PHASE 1 (database only, atomic): reserve stock for every item and
        //   create the Order as Pending. Any failure rolls back ALL of it.
        // PHASE 2 (Stripe, external): create the PaymentIntent. If that fails,
        //   Phase 1 is already committed, so we undo it manually.
        // ─────────────────────────────────────────────────────────
        public async Task<CreateOrderResultDto> CreateOrderWithPaymentIntentAsync(CreateOrderDto dto, string userId)
        {
            // ── PHASE 1 ──
            // Everything inside this lambda runs in one transaction via
            // ExecuteInTransactionAsync (see UnitOfWork.cs for why that wrapper
            // is required with EnableRetryOnFailure).
            //
            // RE-RUN SAFETY: on a transient SQL failure EF may run this lambda
            // again from the top, so the Order is built INSIDE it (a fresh
            // object each attempt), and nothing non-database (like Stripe)
            // happens in here.
            var order = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var item in dto.Items)
                {
                    // Atomic check-and-decrement: succeeds only if enough stock
                    // exists at the instant of the UPDATE, so two buyers racing
                    // for the last unit can't both win.
                    var decremented = await _unitOfWork.ProductRepository
                        .TryDecrementStockAsync(item.ProductId, item.Quantity);

                    if (!decremented)
                    {
                        var product = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductId);

                        // Throwing rolls back the transaction, undoing any
                        // earlier items in this loop that already decremented.
                        // BusinessRuleException is not a transient SQL error, so
                        // EF won't retry it — it goes straight to the caller (400).
                        throw new BusinessRuleException(
                            $"Not enough stock for '{product?.Name ?? $"product {item.ProductId}"}'. " +
                            $"Available: {product?.StockQuantity ?? 0}, Requested: {item.Quantity}");
                    }

                    // If that brought stock to 0, remove this product from
                    // other users' carts so it can't be bought while unavailable.
                    var updatedProduct = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductId);
                    if (updatedProduct != null && updatedProduct.StockQuantity == 0)
                    {
                        await _unitOfWork.OrderRepository.RemoveStaleCartItemsAsync(item.ProductId, userId);
                    }
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
                    OrderItems = dto.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                    TotalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice)
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
                // Compensating action: Phase 1 already committed, so put the
                // stock back and cancel the order manually.
                foreach (var item in order.OrderItems)
                {
                    await _unitOfWork.ProductRepository.RestoreStockAsync(item.ProductId, item.Quantity);
                }

                order.Status = OrderStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync();

                throw;
            }
        }

        // Stock was already reserved at creation, so paying just flips status.
        public async Task MarkOrderPaidAsync(string paymentIntentId)
        {
            var order = await _unitOfWork.OrderRepository.GetByPaymentIntentIdAsync(paymentIntentId);
            if (order == null) return;
            if (order.Status == OrderStatus.Paid) return;

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        // NOTE: there is intentionally no MarkOrderFailedAsync. A declined card leaves
        // the Stripe PaymentIntent open for a retry, so a failed attempt must not
        // release stock. Abandoned orders are released by ExpireStaleOrdersAsync.

        // Called by OrderExpirationBackgroundService: cancels Pending orders
        // older than `olderThan` and releases the stock they were holding.
        public async Task<int> ExpireStaleOrdersAsync(TimeSpan olderThan)
        {
            var cutoff = DateTime.UtcNow - olderThan;
            var staleOrders = await _unitOfWork.OrderRepository.GetStalePendingOrdersAsync(cutoff);

            foreach (var order in staleOrders)
            {
                foreach (var item in order.OrderItems)
                {
                    await _unitOfWork.ProductRepository.RestoreStockAsync(item.ProductId, item.Quantity);
                }

                order.Status = OrderStatus.Cancelled;
            }

            await _unitOfWork.SaveChangesAsync();
            return staleOrders.Count;
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