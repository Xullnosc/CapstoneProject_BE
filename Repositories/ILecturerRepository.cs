using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Repositories
{
    public interface ILecturerRepository
    {
        Task<IEnumerable<Lecturer>> GetAllAsync();
        Task<Lecturer?> GetByIdAsync(int id);
        Task<Lecturer?> GetByEmailAsync(string email);
        Task<PagedResult<Lecturer>> GetByCampusAsync(string campus, int pageIndex, int pageSize);
        Task<IEnumerable<Lecturer>> GetActiveLecturersAsync();
        Task<IEnumerable<Lecturer>> GetReviewersAsync();
        Task AddAsync(Lecturer lecturer);
        Task UpdateAsync(Lecturer lecturer);
        Task DeleteAsync(Lecturer lecturer);
        Task<IEnumerable<Lecturer>> SearchAsync(string term);
        Task<IEnumerable<Lecturer>> GetByEmailsAsync(List<string> emails);
    }
}
