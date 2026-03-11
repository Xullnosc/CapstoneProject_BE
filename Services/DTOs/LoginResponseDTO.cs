using System.Text.Json.Serialization;

namespace Services.DTOs;

public class LoginResponseDTO
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!; // backward compat: same as AccessToken

    [JsonPropertyName("userInfo")]
    public UserInfoDTO UserInfo { get; set; } = null!;
}
