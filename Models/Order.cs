namespace CardShop.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentProvider { get; set; }
        public string TransactionId { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
