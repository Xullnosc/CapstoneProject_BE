using BusinessObjects.Models;

namespace DataAccess
{
    public interface IWhitelistDAO
    {
        Task<Whitelist?> GetByEmailAsync(string email);
        Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId);
        Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists);
    }
}
