namespace api.DTOs.TradeIn
{
    public class TradeInItemDto
    {
        public int Id { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string SetCode { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal? FinalPrice { get; set; }
    }
}
