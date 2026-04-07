using BusinessObjects.Models;

namespace Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetValidByTokenHashAsync(string tokenHash);
    Task RevokeByIdAsync(int id);
    Task RevokeAllByUserIdAsync(int userId);
}
