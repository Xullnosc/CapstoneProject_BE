using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IChecklistDAO
    {
        Task<List<Checklist>> GetAllAsync();
        Task<Checklist?> GetByIdAsync(int id);
        Task<Checklist> AddAsync(Checklist checklist);
        Task UpdateAsync(Checklist checklist);
        Task DeleteAsync(int id);
    }
}
