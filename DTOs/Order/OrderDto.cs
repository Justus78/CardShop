using api.Models;
using CardShop.Models;

namespace api.DTOs.Order
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? PaymentIntentId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string RecipientName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentProvider { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }
}
