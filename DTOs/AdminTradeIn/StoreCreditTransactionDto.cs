namespace api.DTOs.AdminTradeIn
{
    public class StoreCreditTransactionDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Credit"; // or "Debit"
        public string? Description { get; set; }
    }

}
