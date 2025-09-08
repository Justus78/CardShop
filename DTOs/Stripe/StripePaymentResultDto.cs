namespace api.DTOs.Stripe
{
    public class StripePaymentResultDto
    {
        public string ClientSecret { get; set; }
        public string PaymentIntentId { get; set; }
    }
}
