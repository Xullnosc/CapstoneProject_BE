using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class RefreshTokenDAO : IRefreshTokenDAO
{
    private readonly FctmsContext _context;

    public RefreshTokenDAO(FctmsContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken> AddAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<RefreshToken?> GetValidByTokenHashAsync(string tokenHash)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > now);
    }

    public async Task RevokeByIdAsync(int id)
    {
        var token = await _context.RefreshTokens.FindAsync(id);
        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        if (activeTokens.Count == 0) return;

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();
    }
}
