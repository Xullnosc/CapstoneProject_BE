namespace Services.DTOs;

/// <summary>
/// Internal result of login/refresh: includes refresh token for cookie. API returns only AccessToken + UserInfo in body.
/// </summary>
public class LoginResultDTO
{
    public string AccessToken { get; set; } = null!;
    public UserInfoDTO UserInfo { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}
