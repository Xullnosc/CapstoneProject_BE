using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface ISemesterRepository
    {
        Task<List<Semester>> GetAllSemestersAsync();
        Task<Semester?> GetSemesterByIdAsync(int id);
        Task<Semester> CreateSemesterAsync(Semester semester);
        Task UpdateSemesterAsync(Semester semester);
        Task<Semester?> GetCurrentSemesterAsync();
        Task<Semester?> GetSemesterByCodeAsync(string code);
        Task<int> GetStudentRoleIdAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<bool> IsOverlapAsync(DateTime start, DateTime end, int? excludeId);
    }
}
