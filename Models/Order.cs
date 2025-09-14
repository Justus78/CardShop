using api.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CardShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        // relationships
        [Required]
        public string? UserId { get; set; }

        [ValidateNever]
        public ApplicationUser User { get; set; }

        [ValidateNever]
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();


        // shipping info
        public string RecipientName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }


        // Payment / Metadata
        public string PaymentIntentId { get; set; }
        public DateTime? CreatedDate { get; set; }

        // Status
        [Required]
        [StringLength(50)]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        [StringLength(50)]
        public string PaymentProvider { get; set; } = "Unspecified";
        

        // total amount of the order
        [Required]
        public decimal? TotalAmount { get; set; }
    }
}
