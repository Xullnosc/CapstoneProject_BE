using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public interface ISystemSettingDAO
    {
        Task<List<SystemSetting>> GetAllAsync();
        Task<SystemSetting?> GetByKeyAsync(string key);
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
    }

    public class SystemSettingDAO : ISystemSettingDAO
    {
        private readonly FctmsContext _context;

        public SystemSettingDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<SystemSetting>> GetAllAsync()
        {
            return await _context.SystemSettings.ToListAsync();
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key)
        {
            return await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        public async Task UpdateAsync(SystemSetting setting)
        {
            var existing = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == setting.SettingKey);
            if (existing != null)
            {
                existing.SettingValue = setting.SettingValue;
                existing.Description = setting.Description;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddAsync(SystemSetting setting)
        {
            await _context.SystemSettings.AddAsync(setting);
            await _context.SaveChangesAsync();
        }
    }
}
