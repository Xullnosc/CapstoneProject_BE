using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface ILecturerService
    {
        Task<IEnumerable<Lecturer>> GetAllLecturersAsync();
        Task<BusinessObjects.DTOs.PagedResult<Lecturer>> GetLecturersPaginatedAsync(int page, int pageSize, string? search = null);
        Task<Lecturer?> GetLecturerByIdAsync(int id);
        Task AddLecturerAsync(Lecturer lecturer);
        Task UpdateLecturerAsync(Lecturer lecturer);
        Task DeleteLecturerAsync(int id);
        Task ToggleLecturerStatusAsync(int id, bool isActive);
    }
}
