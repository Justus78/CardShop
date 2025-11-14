using api.Models;

namespace api.DTOs.TradeIn
{
    public class TradeInCreateDto
    {
        public List<TradeInItemCreateDto> Items { get; set; } = [];
    }

    public class TradeInItemCreateDto
    {
        public string CardName { get; set; } = string.Empty;
        public string SetCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Condition { get; set; } = "NearMint";
        public decimal EstimatedPrice { get; set; } // per unit from Scryfall
    }

    public class TradeInDto
    {
        public int Id { get; set; }
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public decimal EstimatedValue { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class TradeInSummaryDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TradeInItemDto
    {
        public int Id { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string SetCode { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal EstimatedUnitValue { get; set; }
        public decimal? FinalUnitValue { get; set; }
    }

    public class TradeInDetailDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TradeInItemDto> Items { get; set; } = [];
    }

    // Admin-specific DTOs
    public class TradeInAdminSummaryDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TradeInDetailsDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TradeInItemDto> Items { get; set; } = [];
    }
}
