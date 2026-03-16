using BusinessObjects.Models;
using DataAccess;

namespace Repositories;

public class AccountDetailRepository : IAccountDetailRepository
{
    private readonly IAccountDetailDAO _dao;

    public AccountDetailRepository(IAccountDetailDAO dao)
    {
        _dao = dao;
    }

    public async Task<AccountDetail?> GetByUserIdAsync(int userId) =>
        await _dao.GetByUserIdAsync(userId);

    public async Task<AccountDetail> AddAsync(AccountDetail entity) =>
        await _dao.AddAsync(entity);

    public async Task UpdateAsync(AccountDetail entity) =>
        await _dao.UpdateAsync(entity);
}
