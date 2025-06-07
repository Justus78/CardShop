namespace CardShop.Models
{
    // Models/Product.cs
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } // Card, Sealed, Accessory
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; }

        public CardVariant CardVariant { get; set; }
        public SealedProduct SealedProduct { get; set; }
        public Accessory Accessory { get; set; }

        public ICollection<CartItem> CartItems { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }

}
