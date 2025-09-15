namespace api.DTOs.Order
{
    public class CreateOrderDto
    {
        public List<CreateOrderItemDto> Items { get; set; }
        public string RecipientName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string PaymentProvider { get; set; }

        public string? PaymentIntentId { get; set; }
    }
}
