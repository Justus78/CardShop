namespace CardShop.Models
{
    public class SealedProduct
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string SetCode { get; set; }
        public string Type { get; set; } // Booster Box, Bundle, etc.
    }
}
