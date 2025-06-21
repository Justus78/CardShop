namespace api.DTOs.Stripe
{
    public class StripePaymentResultDto
    {
        public string ClientSecret { get; set; }
        public int OrderId { get; set; }
    }
}
