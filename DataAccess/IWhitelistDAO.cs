using BusinessObjects.Models;

namespace DataAccess
{
    public interface IWhitelistDAO
    {
        Task<Whitelist?> GetByEmailAsync(string email);
        Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId);
        Task<List<Whitelist>> GetByRoleAsync(int roleId);
        Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists);
    }
}
