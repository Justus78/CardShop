namespace api.DTOs.AdminTradeIn
{
    public class StoreCreditDto
    {
        public decimal Balance { get; set; }
        public List<StoreCreditTransactionDto> Transactions { get; set; } = new();
    }
}
