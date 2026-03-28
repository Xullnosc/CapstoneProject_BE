using Services.DTOs;

namespace Services;

public interface IAuthService
{
    Task<LoginResultDTO> GoogleLoginAsync(LoginRequestDTO request);
    Task<LoginResultDTO> CredentialLoginAsync(CredentialLoginRequestDTO request);
    Task<RefreshResultDTO?> RefreshTokenAsync(string? refreshTokenFromCookie);
    Task RevokeRefreshTokenAsync(string? refreshTokenFromCookie);
    Task UpdatePasswordAsync(int userId, string newPassword);
}
