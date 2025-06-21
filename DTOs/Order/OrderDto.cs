namespace api.DTOs.Order
{
    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentProvider { get; set; }
        public string? TransactionId { get; set; }
        public List<OrderItemDto>? Items { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
