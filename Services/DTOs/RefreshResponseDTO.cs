using System.Text.Json.Serialization;

namespace Services.DTOs;

public class RefreshResponseDTO
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = null!;
}

/// <summary>Internal result for refresh: includes new refresh token for cookie.</summary>
public class RefreshResultDTO
{
    public string AccessToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}
