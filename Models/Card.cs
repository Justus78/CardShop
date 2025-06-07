namespace CardShop.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string ScryfallId { get; set; }
        public string Name { get; set; }
        public string SetCode { get; set; }
        public string CollectorNumber { get; set; }

        public ICollection<CardVariant> Variants { get; set; }
    }
}
