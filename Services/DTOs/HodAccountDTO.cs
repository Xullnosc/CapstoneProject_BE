using System.Text.Json.Serialization;

namespace Services.DTOs;

public class HodAccountDTO
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("hasCredential")]
    public bool HasCredential { get; set; }

    [JsonPropertyName("lastLogin")]
    public DateTime? LastLogin { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("campus")]
    public string? Campus { get; set; }
}

