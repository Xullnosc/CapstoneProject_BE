using BusinessObjects.Models;

namespace Repositories
{
    public interface ISystemParameterRepository
    {
        Task<List<SystemParameter>> GetAllParametersAsync();
        Task<SystemParameter?> GetParameterByKeyAsync(string key);
        Task UpdateParameterAsync(SystemParameter parameter);
    }
}
