using static api.Enums.ProductEnums;

namespace api.Models
{
    public class TradeInItem
    {
        public int Id { get; set; }
        public string? CardName { get; set;}
        public string? SetCode { get; set; }
        public int Quantity { get; set; }
        public CardCondition Condition { get; set; } = CardCondition.NearMint;
        public decimal EstimatedUnitValue { get; set; }
        public decimal FinalUnitValue { get; set; }

        // FK for trade in //
        public int TradeInId { get; set; }

        // Navigation Property for trade in //
        public TradeIn? TradeIn { get; set; }


    } // end trade in item model
}
