using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AccountDetailDAO : IAccountDetailDAO
{
    private readonly FctmsContext _context;

    public AccountDetailDAO(FctmsContext context)
    {
        _context = context;
    }

    public async Task<AccountDetail?> GetByUserIdAsync(int userId)
    {
        return await _context.AccountDetails
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<AccountDetail> AddAsync(AccountDetail entity)
    {
        await _context.AccountDetails.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(AccountDetail entity)
    {
        _context.AccountDetails.Update(entity);
        await _context.SaveChangesAsync();
    }
}
