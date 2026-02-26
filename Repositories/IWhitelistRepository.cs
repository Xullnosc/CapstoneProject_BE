using BusinessObjects.Models;

namespace Repositories
{
    public interface IWhitelistRepository
    {
        Task<Whitelist?> GetByEmailAsync(string email);
        Task<IEnumerable<Whitelist>> GetByRoleAsync(int roleId);
        Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId);
        Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists);
        Task<Whitelist?> GetByIdAsync(int id);
        Task UpdateAsync(Whitelist whitelist);
    }
}
