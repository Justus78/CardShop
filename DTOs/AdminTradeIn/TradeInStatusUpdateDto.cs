namespace api.DTOs.AdminTradeIn
{
    public class TradeInStatusUpdateDto
    {
        public int TradeInId { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Received, Finalized, Cancelled
        public string? AdminNotes { get; set; }
    }
}
