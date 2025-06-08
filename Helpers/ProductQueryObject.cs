namespace api.Helpers
{
    public class ProductQueryObject
    {
        public string? SortBy { get; set; } = "Id";       // name, price, category, etc.
        public bool Ascending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Optional future filtering
        public string? Category { get; set; }
        public string? Rarity { get; set; }
        public bool? IsFoil { get; set; }
    }
}
