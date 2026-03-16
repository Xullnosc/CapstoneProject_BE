using BusinessObjects.Models;

namespace DataAccess;

public interface IAccountDetailDAO
{
    Task<AccountDetail?> GetByUserIdAsync(int userId);
    Task<AccountDetail> AddAsync(AccountDetail entity);
    Task UpdateAsync(AccountDetail entity);
}
