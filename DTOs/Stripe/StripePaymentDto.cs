namespace api.DTOs.Stripe
{
    public class StripePaymentDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
    }
}
