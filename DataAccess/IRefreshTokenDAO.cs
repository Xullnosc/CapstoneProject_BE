using BusinessObjects.Models;

namespace DataAccess;

public interface IRefreshTokenDAO
{
    Task<RefreshToken> AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetValidByTokenHashAsync(string tokenHash);
    Task RevokeByIdAsync(int id);
    Task RevokeAllByUserIdAsync(int userId);
}
