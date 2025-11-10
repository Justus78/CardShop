using CardShop.Models;

namespace api.Models
{
    public class TradeIn
    {
        public int Id { get; set; }
        public TradeInStatus Status { get; set; }
        public decimal? EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }    
        public DateTime? ReceivedAt { get; set;}
        public DateTime? AutoCreditDate { get; set; }

        // FK for user //
        public string UserId { get; set; }

        // Navigation property for user //
        public ApplicationUser? User { get; set; }


        // navigation property for trade in items
        public List<TradeInItem> TradeInItems { get; set; } = [];
    } // end trade in model
}
