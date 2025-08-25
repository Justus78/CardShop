using System.Text.Json.Serialization;

namespace api.DTOs.Sets
{
    public class ScryfallResponse
    {
        [JsonPropertyName("data")]
        public List<ScryfallSetDto>? Data { get; set; }
    }

}
