using System.Text.Json.Serialization;

namespace api.DTOs.Sets
{
    public class ScryfallSetDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon_svg_uri")]
        public string IconSvgUri { get; set; }

        [JsonPropertyName("released_at")]
        public DateTime? ReleasedAt { get; set; }
    }

}
