using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface ISemesterRepository
    {
        Task<List<Semester>> GetAllSemestersAsync();
        Task<PagedResult<Semester>> GetAllSemestersAsync(int pageIndex, int pageSize);
        Task<Semester?> GetSemesterByIdAsync(int id);
        Task<Semester?> GetSemesterByIdSimpleAsync(int id);
        Task<Semester> CreateSemesterAsync(Semester semester);
        Task UpdateSemesterAsync(Semester semester);
        Task UpdateMidtermReviewAsync(int semesterId, DateTime lockDate);
        Task<Semester?> GetCurrentSemesterAsync();
        Task<Semester?> GetSemesterByCodeAsync(string code);
        Task<int> GetStudentRoleIdAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<Semester?> IsOverlapAsync(DateTime start, DateTime end, int? excludeId);
        Task<bool> HasActiveSemesterAsync();
        Task<bool> SemesterExistsAsync(int semesterId);
        Task<PagedResult<Whitelist>> GetOrphanedStudentsAsync(int semesterId, int pageIndex, int pageSize, string? search = null);
        Task<List<Semester>> GetPreviousClosedSemestersAsync(int currentSemesterId, int count);
    }
}

