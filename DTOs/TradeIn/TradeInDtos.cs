using api.Models;
using static api.Enums.ProductEnums;

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
        public CardCondition Condition { get; set; } = CardCondition.NearMint;
        public decimal? EstimatedPrice { get; set; } // per unit from Scryfall
    }

    // Add single item to an existing trade-in
    public class AddTradeInItemDto
    {
        public string CardName { get; set; } = string.Empty;
        public string SetCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public CardCondition Condition { get; set; } = CardCondition.NearMint;
        public decimal? EstimatedUnitValue { get; set; }
    }

    // Update item in existing trade-in
    public class UpdateTradeInItemDto
    {
        public int Quantity { get; set; }
        public CardCondition Condition { get; set; }
    }

    public class TradeInDto
    {
        public int Id { get; set; }
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public decimal? EstimatedValue { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }

    public class TradeInSummaryDto
    {
        public int Id { get; set; }
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public decimal? EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TradeInItem>? Items { get; set; }
    }

    public class TradeInItemDto
    {
        public int Id { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string SetCode { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? EstimatedUnitValue { get; set; }
        public decimal? FinalUnitValue { get; set; }
    }

    public class TradeInDetailDto
    {
        public int Id { get; set; }
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public decimal? EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TradeInItemDto> Items { get; set; } = [];
    }

    // Pending trade-in for frontend
    public class TradeInPendingDto
    {
        public int Id { get; set; }
        public List<TradeInItemDto> Items { get; set; } = [];
    }

    // Admin-specific DTOs
    public class TradeInAdminSummaryDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public decimal? EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TradeInDetailsDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public TradeInStatus Status { get; set; } = TradeInStatus.Submitted;
        public decimal? EstimatedValue { get; set; }
        public decimal? FinalValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TradeInItemDto> Items { get; set; } = [];
    }

    public class UpdateTradeInStatusDto
    {
        // New status to set for the trade-in
        public TradeInStatus Status { get; set; }
        // Optional admin note (why status changed)
        public string? AdminNote { get; set; }
    }

    public class UpdateTradeInItemValueDto
    {
        // The final per-unit value that admin assigns to this item
        public decimal FinalUnitValue { get; set; }
    }
}
