namespace api.Helpers
{
    public static class TradeCodeGenerator
    {
        private static readonly Random _random = new();

        public static string Generate()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var randomPart = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            return $"TRD-{datePart}-{randomPart}";
        }
    }
}
