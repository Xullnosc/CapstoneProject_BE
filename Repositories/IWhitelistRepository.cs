using BusinessObjects.Models;

namespace Repositories
{
    public interface IWhitelistRepository
    {
        Task<Whitelist?> GetByEmailAsync(string email);
        Task<IEnumerable<Whitelist>> GetByRoleAsync(int roleId);
    }
}
