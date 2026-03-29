using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface ICampusRepository
    {
        Task<List<Campus>> GetAllAsync();
        Task<Campus?> GetByIdAsync(int id);
        Task<Campus?> GetByCodeAsync(string code);
        Task<Campus> AddAsync(Campus campus);
        Task UpdateAsync(Campus campus);
        Task DeleteAsync(Campus campus);
        Task<bool> HasActiveReferencesAsync(int campusId);
    }
}
