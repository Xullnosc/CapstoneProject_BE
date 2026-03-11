using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface ILecturerRepository
    {
        Task<IEnumerable<Lecturer>> GetAllAsync();
        Task<Lecturer?> GetByIdAsync(int id);
        Task<Lecturer?> GetByEmailAsync(string email);
        Task<PagedResult<Lecturer>> GetByCampusAsync(string campus, int pageIndex, int pageSize);
        Task<IEnumerable<Lecturer>> GetActiveLecturersAsync();
        Task AddAsync(Lecturer lecturer);
        Task UpdateAsync(Lecturer lecturer);
        Task DeleteAsync(Lecturer lecturer);
        Task<IEnumerable<Lecturer>> SearchAsync(string term);
    }
}
