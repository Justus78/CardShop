namespace api.DTOs.TradeIn
{
    public class TradeInItemCreateDto
    {
        public string CardName { get; set; } = string.Empty;
        public string SetCode { get; set; } = string.Empty; // e.g. "ONE", "MH2"
        public string Condition { get; set; } = "Near Mint";
        public int Quantity { get; set; }
        public decimal EstimatedPrice { get; set; }
    }
}
