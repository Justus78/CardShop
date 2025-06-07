namespace CardShop.Models
{
    public class CardVariant
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public Card Card { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Finish { get; set; }
        public string Language { get; set; }
        public string Condition { get; set; }
    }
}
