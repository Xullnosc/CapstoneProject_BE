using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface ICampusService
    {
        Task<List<CampusDTO>> GetAllCampusesAsync();
        Task<CampusDTO?> GetCampusByIdAsync(int campusId);
        Task<CampusDTO> CreateCampusAsync(CreateCampusDTO dto);
        Task<CampusDTO> UpdateCampusAsync(int campusId, UpdateCampusDTO dto);
        Task DeleteCampusAsync(int campusId);
    }
}
