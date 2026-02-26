using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class WhitelistRepository : IWhitelistRepository
    {
        private readonly IWhitelistDAO _whitelistDAO;

        public WhitelistRepository(IWhitelistDAO whitelistDAO)
        {
            _whitelistDAO = whitelistDAO;
        }

        public async Task<Whitelist?> GetByEmailAsync(string email)
        {
            return await _whitelistDAO.GetByEmailAsync(email);
        }

        public async Task<IEnumerable<Whitelist>> GetByRoleAsync(int roleId)
        {
            return await _whitelistDAO.GetByRoleAsync(roleId);
        }

        public async Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId)
        {
            return await _whitelistDAO.GetBySemesterIdAsync(semesterId);
        }
        public async Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists)
        {
            await _whitelistDAO.DeleteRangeAsync(whitelists);
        }
    }
}
