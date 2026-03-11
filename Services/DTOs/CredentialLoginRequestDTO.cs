using System.Text.Json.Serialization;

namespace Services.DTOs;

public class CredentialLoginRequestDTO
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("password")]
    public string Password { get; set; } = null!;
}
