using System.Text.Json.Serialization;

namespace Services.DTOs
{
    public class LoginRequestDTO
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = null!;

        [JsonPropertyName("campus")]
        public string Campus { get; set; } = null!;
    }
}
