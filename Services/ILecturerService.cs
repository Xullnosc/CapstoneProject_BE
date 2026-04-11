using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface ILecturerService
    {
        Task<IEnumerable<Lecturer>> GetAllLecturersAsync();
        Task<BusinessObjects.DTOs.PagedResult<Lecturer>> GetLecturersPaginatedAsync(int page, int pageSize, string? search = null, int? campusId = null);
        Task<PagedResult<Lecturer>> GetLecturersByCampusAsync(string campus, int pageIndex, int pageSize);
        Task<Lecturer?> GetLecturerByIdAsync(int id);
        Task AddLecturerAsync(Lecturer lecturer);
        Task UpdateLecturerAsync(Lecturer lecturer);
        Task DeleteLecturerAsync(int id);
        Task ToggleLecturerStatusAsync(int id, bool isActive);
        Task ToggleReviewerAsync(int id, bool isReviewer);
        Task ToggleHodAsync(int id, bool isHod);
    }
}
