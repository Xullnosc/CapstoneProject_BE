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

    public async Task<SystemUserCredential?> GetByUserIdAsync(int userId)
    {
        return await _context.SystemUserCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId);
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
}
