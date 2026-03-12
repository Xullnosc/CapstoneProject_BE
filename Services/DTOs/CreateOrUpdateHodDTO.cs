using System.Text.Json.Serialization;

namespace Services.DTOs;

public class CreateOrUpdateHodDTO
{
    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = null!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("password")]
    public string Password { get; set; } = null!;

    [JsonPropertyName("campus")]
    public string? Campus { get; set; }
}
