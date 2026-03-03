using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IChecklistService
    {
        Task<List<ChecklistDTO>> GetAllAsync();
        Task<ChecklistDTO?> GetByIdAsync(int id);
        Task<ChecklistDTO> CreateAsync(ChecklistCreateDTO dto);
        Task UpdateAsync(int id, ChecklistUpdateDTO dto);
        Task DeleteAsync(int id);
    }
}
