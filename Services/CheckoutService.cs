using api.DTOs.Order;
using api.DTOs.Stripe;
using api.Interfaces;
using CardShop.Data;
using Stripe;
using Stripe.Checkout;

namespace api.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;

        public CheckoutService(IOrderService orderService, ApplicationDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        public async Task<StripePaymentResultDto> CreatePaymentAndOrderAsync(CreateOrderDto orderDto, string userId)
        {
            // 1. Create Stripe PaymentIntent
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(orderDto.Items.Sum(i => i.UnitPrice * i.Quantity) * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" }
            };

            var intent = await new PaymentIntentService().CreateAsync(options);

            // 2. Create Order using your existing logic
            var createdOrder = await _orderService.CreateOrderAsync(orderDto, userId);

            // 3. Update order with TransactionId (Stripe PaymentIntent.Id)
            var orderToUpdate = await _context.Orders.FindAsync(createdOrder.Id);
            if (orderToUpdate != null)
            {
                orderToUpdate.TransactionId = intent.Id;
                await _context.SaveChangesAsync();
            }

            // 4. Return result
            return new StripePaymentResultDto
            {
                ClientSecret = intent.ClientSecret,
                OrderId = createdOrder.Id
            };
        }
    }


}
