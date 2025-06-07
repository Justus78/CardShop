namespace CardShop.Models
{
    public class Accessory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Type { get; set; } // Sleeves, Deck Box, etc.
        public string Brand { get; set; }
    }
}
