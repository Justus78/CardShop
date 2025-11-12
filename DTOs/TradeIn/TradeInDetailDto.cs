using api.Models;

namespace api.DTOs.TradeIn
{
public class TradeInDetailDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public TradeInStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime SubmittedAt { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public decimal? EstimatedTotalValue { get; set; }
        public decimal? FinalValue { get; set; }
        public string? Notes { get; set; }
        public List<TradeInItemDto> Items { get; set; } = new();
    }
}
