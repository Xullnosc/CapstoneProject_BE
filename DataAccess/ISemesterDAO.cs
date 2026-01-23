using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface ISemesterDAO
    {
        Task<List<Semester>> GetAllAsync();
        Task<Semester?> GetByIdAsync(int id);
        Task<Semester> AddAsync(Semester semester);
        Task UpdateAsync(Semester semester);
        Task DeleteAsync(int id);
        Task<Semester?> GetCurrentSemesterAsync();
    }
}
