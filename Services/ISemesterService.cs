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
        Task EndSemesterAsync(int id);
    }
}
