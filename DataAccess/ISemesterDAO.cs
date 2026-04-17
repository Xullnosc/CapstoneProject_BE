using BusinessObjects.DTOs;
using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface ISemesterDAO
    {
        Task<List<Semester>> GetAllAsync();
        Task<PagedResult<Semester>> GetAllAsync(int pageIndex, int pageSize);
        Task<Semester?> GetByIdAsync(int id);
        Task<Semester?> GetByIdSimpleAsync(int id);
        Task<Semester> AddAsync(Semester semester);
        Task UpdateAsync(Semester semester);
        Task<Semester?> GetCurrentSemesterAsync();
        Task<Semester?> GetByCodeAsync(string code);
        Task<int> GetStudentRoleIdAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<PagedResult<Role>> GetAllRolesAsync(int pageIndex, int pageSize);
        Task<Semester?> IsOverlapAsync(DateTime start, DateTime end, int? excludeId);
        Task<PagedResult<Whitelist>> GetOrphanedStudentsAsync(int semesterId, int pageIndex, int pageSize, string? search = null);
              Task<bool> HasActiveSemesterAsync();
        Task<List<Semester>> GetPreviousClosedSemestersAsync(int currentSemesterId, int count);
    }
}
