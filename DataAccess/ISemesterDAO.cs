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
        Task<Semester> AddAsync(Semester semester);
        Task UpdateAsync(Semester semester);
        Task<Semester?> GetCurrentSemesterAsync();
        Task<Semester?> GetByCodeAsync(string code);
        Task<int> GetStudentRoleIdAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<PagedResult<Role>> GetAllRolesAsync(int pageIndex, int pageSize);
        Task<bool> IsOverlapAsync(DateTime start, DateTime end, int? excludeId);
    }
}
