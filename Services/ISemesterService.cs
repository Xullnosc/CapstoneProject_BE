using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface ISemesterService
    {
        Task<List<SemesterDTO>> GetAllSemestersAsync();
        Task<PagedResult<SemesterDTO>> GetAllSemestersPaginatedAsync(int page, int pageSize);
        Task<SemesterDTO?> GetSemesterByIdAsync(int id);
        Task<SemesterDTO?> GetCurrentSemesterAsync();
        Task<SemesterDTO> CreateSemesterAsync(SemesterCreateDTO semesterCreateDTO);
        Task UpdateSemesterAsync(SemesterCreateDTO semesterCreateDTO);
        Task StartSemesterAsync(int id);
        Task LockSubmissionAsync(int id);
        Task LockAllUpdatesAsync(int id);
        Task AnnounceMidtermReviewAsync(int id, DateTime lockDate);
        Task CloseSemesterAsync(int id);
        Task<PagedResult<WhitelistDTO>> GetWhitelistsPaginatedAsync(int semesterId, int page, int pageSize, string? role = null, string? search = null);
        Task<PagedResult<WhitelistDTO>> GetOrphanedStudentsAsync(int semesterId, int page, int pageSize, string? search = null);
        Task InvalidateSemesterCacheAsync(int? id = null);
    }
}
