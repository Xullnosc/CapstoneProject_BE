using BusinessObjects.Models;

namespace DataAccess
{
    public interface ISystemParameterDAO
    {
        Task<List<SystemParameter>> GetAllParametersAsync();
        Task<SystemParameter?> GetParameterByKeyAsync(string key);
        Task UpdateParameterAsync(SystemParameter parameter);
    }
}
