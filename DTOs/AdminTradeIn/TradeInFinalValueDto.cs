namespace api.DTOs.AdminTradeIn
{
    public class TradeInFinalValueDto
    {
        public int TradeInId { get; set; }
        public decimal FinalValue { get; set; }
        public List<TradeInItemFinalValueDto>? UpdatedItems { get; set; } // optional item-level detail
    }
}

