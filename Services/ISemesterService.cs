using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface ISemesterService
    {
        Task<List<SemesterDTO>> GetAllSemestersAsync();
        Task<SemesterDTO?> GetSemesterByIdAsync(int id);
        Task<SemesterDTO> CreateSemesterAsync(SemesterCreateDTO semesterCreateDTO);
        Task UpdateSemesterAsync(SemesterCreateDTO semesterCreateDTO);
        Task StartSemesterAsync(int id);
        Task EndSemesterAsync(int id);
        Task<PagedResult<WhitelistDTO>> GetWhitelistsPaginatedAsync(int semesterId, int page, int pageSize, string? role = null, string? search = null);
        Task InvalidateSemesterCacheAsync(int? id = null);
    }
}
