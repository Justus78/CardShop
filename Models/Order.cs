using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CardShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [ValidateNever]
        public ApplicationUser User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        [StringLength(50)]
        public string PaymentProvider { get; set; } = "Unspecified";

        [Required]
        [StringLength(100)]
        public string? TransactionId { get; set; }

        [ValidateNever]
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [ValidateNever]
        public decimal? TotalAmout => OrderItems?.Sum(i => i.Quantity * i.UnitPrice);
    }
}
