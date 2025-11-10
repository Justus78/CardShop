namespace api.Models
{
    public class StoreCreditTransaction
    {
        public int Id { get; set; }
        public int StoreCreditId { get; set; } // FK for store credit
        public decimal ChangeAmount { get; set; }   // add or subtract from credit
        public decimal NewBalance { get; set; } = 0; // new credit balance
        public StoreCreditSource Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property for Store Credit //
        public StoreCredit? StoreCredit { get; set; }
    }
}
