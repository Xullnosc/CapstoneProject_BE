using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class SystemParameterRepository : ISystemParameterRepository
    {
        private readonly ISystemParameterDAO _systemParameterDAO;

        public SystemParameterRepository(ISystemParameterDAO systemParameterDAO)
        {
            _systemParameterDAO = systemParameterDAO;
        }

        public async Task<List<SystemParameter>> GetAllParametersAsync()
        {
            return await _systemParameterDAO.GetAllParametersAsync();
        }

        public async Task<SystemParameter?> GetParameterByKeyAsync(string key)
        {
            return await _systemParameterDAO.GetParameterByKeyAsync(key);
        }

        public async Task UpdateParameterAsync(SystemParameter parameter)
        {
            await _systemParameterDAO.UpdateParameterAsync(parameter);
        }
    }
}
