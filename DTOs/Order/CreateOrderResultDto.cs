namespace api.DTOs.Order
{
    public class CreateOrderResultDto
    {
        public string ClientSecret { get; set; } = null!;
        public string PaymentIntentId { get; set; } = null!;
        public OrderDto Order { get; set; } = null!;
    }
}
