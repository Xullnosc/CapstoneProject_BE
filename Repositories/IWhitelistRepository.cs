using BusinessObjects.Models;

namespace Repositories
{
    public interface IWhitelistRepository
    {
        Task<Whitelist?> GetByEmailAsync(string email);

        Task AddRangeAsync(IEnumerable<Whitelist> whitelists);
    }
}
