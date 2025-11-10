namespace api.DTOs.TradeIn
{
    public class TradeInCreateDto
    {
        public List<TradeInItemCreateDto> Items { get; set; } = new();
        public string Notes { get; set; } = string.Empty; // optional message or shipping info
    }
}
}
