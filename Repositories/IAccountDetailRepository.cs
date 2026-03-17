using BusinessObjects.Models;

namespace Repositories;

public interface IAccountDetailRepository
{
    Task<AccountDetail?> GetByUserIdAsync(int userId);
    Task<AccountDetail> AddAsync(AccountDetail entity);
    Task UpdateAsync(AccountDetail entity);
}
