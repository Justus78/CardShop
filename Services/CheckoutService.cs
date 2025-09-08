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
        public async Task<StripePaymentResultDto> CreatePaymentIntentAsync(CreateOrderDto orderDto, string userId)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(orderDto.Items.Sum(i => i.UnitPrice * i.Quantity) * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" }
            };

            var intent = await new PaymentIntentService().CreateAsync(options);

            return new StripePaymentResultDto
            {
                ClientSecret = intent.ClientSecret,
                PaymentIntentId = intent.Id
            };
        }
    }



}
