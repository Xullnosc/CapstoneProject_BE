using BusinessObjects.Models;
using DataAccess;

namespace Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IRefreshTokenDAO _dao;

    public RefreshTokenRepository(IRefreshTokenDAO dao)
    {
        _dao = dao;
    }

    public Task<RefreshToken> AddAsync(RefreshToken refreshToken) =>
        _dao.AddAsync(refreshToken);

    public Task<RefreshToken?> GetValidByTokenHashAsync(string tokenHash) =>
        _dao.GetValidByTokenHashAsync(tokenHash);

    public Task RevokeByIdAsync(int id) =>
        _dao.RevokeByIdAsync(id);
}
