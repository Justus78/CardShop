namespace api.DTOs.TradeIn
{
    public class TradeInSummaryDto
    {

        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public decimal EstimatedTotalValue { get; set; }
        public decimal? FinalValue { get; set; }
        public string? Notes { get; set; }
    }
}
