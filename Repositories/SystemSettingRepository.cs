using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public interface ISystemSettingRepository
    {
        Task<List<SystemSetting>> GetAllAsync();
        Task<SystemSetting?> GetByKeyAsync(string key);
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
    }

    public class SystemSettingRepository : ISystemSettingRepository
    {
        private readonly ISystemSettingDAO _dao;

        public SystemSettingRepository(ISystemSettingDAO dao)
        {
            _dao = dao;
        }

        public async Task<List<SystemSetting>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key)
        {
            return await _dao.GetByKeyAsync(key);
        }

        public async Task UpdateAsync(SystemSetting setting)
        {
            await _dao.UpdateAsync(setting);
        }

        public async Task AddAsync(SystemSetting setting)
        {
            await _dao.AddAsync(setting);
        }
    }
}
