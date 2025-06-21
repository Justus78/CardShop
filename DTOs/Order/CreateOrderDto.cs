namespace api.DTOs.Order
{
    public class CreateOrderDto
    {
        public List<OrderItemDto> Items { get; set; }
        public string PaymentProvider { get; set; } = "Stripe"; // "Stripe", "PayPal", etc.
        public string? TransactionId { get; set; } // populated after payment
    }
}
