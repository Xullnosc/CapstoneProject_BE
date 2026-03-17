using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface IWhitelistDAO
    {
        Task<Whitelist?> GetByEmailAsync(string email);
        Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId);
        Task<PagedResult<Whitelist>> GetBySemesterIdAsync(int semesterId, int pageIndex, int pageSize);
        Task<List<Whitelist>> GetByRoleAsync(int roleId);
        Task<PagedResult<Whitelist>> GetByRoleAsync(int roleId, int pageIndex, int pageSize);
        Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists);
        Task AddRangeAsync(IEnumerable<Whitelist> whitelists);
        Task ReplaceStudentsBySemesterAsync(int semesterId, int studentRoleId, IEnumerable<Whitelist> newStudents);
        Task<Whitelist?> GetByIdAsync(int id);
        Task UpdateAsync(Whitelist whitelist);
        Task AddAsync(Whitelist whitelist);
        Task DeleteAsync(Whitelist whitelist);
        Task<List<Whitelist>> SearchAsync(string term, int semesterId);
        Task<bool> IsWhitelistedInSemesterAsync(string email, int semesterId);
    }
}
