using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public interface ISystemSettingService
    {
        Task<List<SystemSetting>> GetAllAsync();
        Task<SystemSetting?> GetByKeyAsync(string key);
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
        Task<string> GetSettingValueAsync(string key, string defaultValue = "N/A");
    }

    public class SystemSettingService : ISystemSettingService
    {
        private readonly ISystemSettingRepository _repository;

        public SystemSettingService(ISystemSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SystemSetting>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key)
        {
            return await _repository.GetByKeyAsync(key);
        }

        public async Task UpdateAsync(SystemSetting setting)
        {
            await _repository.UpdateAsync(setting);
        }

        public async Task AddAsync(SystemSetting setting)
        {
            await _repository.AddAsync(setting);
        }

        public async Task<string> GetSettingValueAsync(string key, string defaultValue = "N/A")
        {
            var setting = await _repository.GetByKeyAsync(key);
            return setting?.SettingValue ?? defaultValue;
        }
    }
}
