using api.Models;

namespace api.DTOs.Order
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string PaymentIntentId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    } // end dto for order response
} // end namespace
