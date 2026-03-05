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

        public async Task AddRangeAsync(IEnumerable<Whitelist> whitelists)
        {
            await _whitelistDAO.AddRangeAsync(whitelists);
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

        public async Task ReplaceStudentsBySemesterAsync(int semesterId, int studentRoleId, IEnumerable<Whitelist> newStudents)
        {
            await _whitelistDAO.ReplaceStudentsBySemesterAsync(semesterId, studentRoleId, newStudents);
        }

        public async Task<Whitelist?> GetByIdAsync(int id)
        {
            return await _whitelistDAO.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Whitelist whitelist)
        {
            await _whitelistDAO.UpdateAsync(whitelist);
        }

        public async Task AddAsync(Whitelist whitelist)
        {
            await _whitelistDAO.AddAsync(whitelist);
        }

        public async Task DeleteAsync(Whitelist whitelist)
        {
            await _whitelistDAO.DeleteAsync(whitelist);
        }

        public async Task<List<Whitelist>> SearchAsync(string term, int semesterId)
        {
            return await _whitelistDAO.SearchAsync(term, semesterId);
        }
    }
}
