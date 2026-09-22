using api.DTOs.Order;
using api.Interfaces;
using api.Models;
using CardShop.Models;
using Stripe;

namespace api.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IConfiguration _config;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IConfiguration config)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _config = config;
        }

        public async Task<CreateOrderResultDto> CreateOrderWithPaymentIntentAsync(CreateOrderDto dto, string userId)
        {
            // Validate stock before ever touching Stripe
            foreach (var item in dto.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Product not found: {item.ProductId}");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");
            }

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(dto.Items.Sum(i => i.Quantity * i.UnitPrice) * 100),
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Shipping = new ChargeShippingOptions
                {
                    Name = dto.ShippingInfo.FullName,
                    Address = new AddressOptions
                    {
                        Line1 = dto.ShippingInfo.Address,
                        City = dto.ShippingInfo.City,
                        State = dto.ShippingInfo.State,
                        PostalCode = dto.ShippingInfo.PostalCode,
                        Country = dto.ShippingInfo.Country
                    }
                }
            };

            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.CreateAsync(options);

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Pending,
                PaymentIntentId = paymentIntent.Id,
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

            var created = await _orderRepository.AddAsync(order);

            return new CreateOrderResultDto
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Order = MapToDto(created)
            };
        }

        public async Task MarkOrderPaidAsync(string paymentIntentId)
        {
            var order = await _orderRepository.GetByPaymentIntentIdAsync(paymentIntentId);
            if (order == null) return;
            if (order.Status == OrderStatus.Paid) return;

            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null) continue;

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Not enough stock for '{product.Name}' at payment time.");

                product.StockQuantity -= item.Quantity;

                if (product.StockQuantity == 0)
                    await _orderRepository.RemoveStaleCartItemsAsync(product.Id, order.UserId);
            }

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;

            await _orderRepository.SaveChangesAsync();
        }

        public async Task MarkOrderFailedAsync(string paymentIntentId)
        {
            var order = await _orderRepository.GetByPaymentIntentIdAsync(paymentIntentId);
            if (order == null) return;
            if (order.Status == OrderStatus.Failed || order.Status == OrderStatus.Paid) return;

            order.Status = OrderStatus.Failed;
            await _orderRepository.SaveChangesAsync();
        }

        public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
        {
            var orders = await _orderRepository.GetForUserAsync(userId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, userId);
            return order == null ? null : MapToDto(order);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            order.Status = newStatus;
            await _orderRepository.SaveChangesAsync();
            return true;
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                Username = order.User.UserName,
                PaymentIntentId = order.PaymentIntentId,
                Status = order.Status,
                CreatedAt = (DateTime)order.CreatedDate,
                PaidAt = order.PaidAt,
                RecipientName = order.RecipientName,
                Street = order.Street,
                City = order.City,
                State = order.State,
                PostalCode = order.PostalCode,
                Country = order.Country,
                TotalAmount = (decimal)order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
    }
}