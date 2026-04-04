using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Services
{
    public interface ISystemParameterService
    {
        Task<List<SystemParameterDTO>> GetAllParametersAsync();
        Task<SystemParameterDTO?> GetParameterByKeyAsync(string key);
        Task UpdateParameterAsync(SystemParameterDTO parameterDto);
        Task<int> GetIntAsync(string key, int fallback = 0);
        Task<bool> GetBoolAsync(string key, bool fallback = true);
    }
}
