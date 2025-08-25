namespace api.Models
{
    public class Set
    {
        public int Id { get; set; }         // Your internal ID
        public string? Code { get; set; }    // e.g. "lea"
        public string? Name { get; set; }    // e.g. "Limited Edition Alpha"
        public string? IconUrl { get; set; } // Scryfall's icon_svg_uri
        public DateTime? ReleasedAt { get; set; }
    }

}
