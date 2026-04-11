using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class SystemUserCredentialDAO : ISystemUserCredentialDAO
{
    private readonly FctmsContext _context;

    public SystemUserCredentialDAO(FctmsContext context)
    {
        _context = context;
    }

    public async Task<SystemUserCredential?> GetByUsernameAsync(string username)
    {
        return await _context.SystemUserCredentials
            .Include(c => c.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(c => c.Username == username);
    }

    public async Task<SystemUserCredential?> GetByIdentifierAsync(string identifier)
    {
        return await _context.SystemUserCredentials
            .Include(c => c.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(c => c.Username == identifier || (c.User != null && c.User.Email == identifier));
    }

    public async Task<SystemUserCredential?> GetByUserIdAsync(int userId)
    {
        return await _context.SystemUserCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<List<SystemUserCredential>> GetByUserIdsAsync(List<int> userIds)
    {
        return await _context.SystemUserCredentials
            .Where(c => userIds.Contains(c.UserId))
            .ToListAsync();
    }

    public async Task<SystemUserCredential> AddAsync(SystemUserCredential credential)
    {
        await _context.SystemUserCredentials.AddAsync(credential);
        await _context.SaveChangesAsync();
        return credential;
    }

    public async Task UpdateAsync(SystemUserCredential credential)
    {
        _context.SystemUserCredentials.Update(credential);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SystemUserCredential credential)
    {
        _context.SystemUserCredentials.Remove(credential);
        await _context.SaveChangesAsync();
    }
}
