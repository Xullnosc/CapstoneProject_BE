using BusinessObjects.Models;

namespace Repositories
{
    public interface IWhitelistRepository
    {
        Task<Whitelist?> GetByEmailAsync(string email);
        Task AddRangeAsync(IEnumerable<Whitelist> whitelists);
        Task<IEnumerable<Whitelist>> GetByRoleAsync(int roleId);
        Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId);
        Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists);
        Task<Whitelist?> GetByIdAsync(int id);
        Task ReplaceStudentsBySemesterAsync(int semesterId, int studentRoleId, IEnumerable<Whitelist> newStudents);
        Task UpdateAsync(Whitelist whitelist);
        Task<List<Whitelist>> SearchAsync(string term, int semesterId);
    }
}

