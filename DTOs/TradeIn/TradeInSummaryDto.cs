using api.Models;

namespace api.DTOs.TradeIn
{
    public class TradeInSummaryDto
    {

        public int Id { get; set; }
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public DateTime SubmittedAt { get; set; }
        public decimal? EstimatedTotalValue { get; set; }
        public decimal? FinalValue { get; set; }
        public string? Notes { get; set; }
    }
}
