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
        Task DeleteSemesterAsync(int id);
        Task<Semester?> GetCurrentSemesterAsync();
    }
}
