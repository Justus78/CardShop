using CardShop.Models;

namespace api.Models
{
    public class StoreCredit
    {
        public int Id { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SourceId { get; set; } // trade in Id

        // navigation properties //
        public List<StoreCreditTransaction> Transactions { get; set; } = [];
        public ApplicationUser? User { get; set; }

        // FK for user //
        public string? UserId { get; set; }


    }
}
