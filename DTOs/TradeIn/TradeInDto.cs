using api.Models;

namespace api.DTOs.TradeIn
{
    public class TradeInDto
    {
        public int Id { get; set; }
        public decimal? EstimatedValue { get; set; }
        public TradeInStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
