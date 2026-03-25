using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Services
{
    public interface ISystemParameterService
    {
        Task<List<SystemParameterDTO>> GetAllParametersAsync();
        Task<SystemParameterDTO?> GetParameterByKeyAsync(string key);
        Task UpdateParameterAsync(SystemParameterDTO parameterDto);
    }
}
