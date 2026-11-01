namespace api.DTOs.Order
{
    public class CreateOrderDto
    {
        public List<OrderItemDto> Items { get; set; }
        public ShippingInfoDto ShippingInfo { get; set; } = new();
        public string? PaymentProvider { get; set; }

        public string? PaymentIntentId { get; set; }
    }
}
