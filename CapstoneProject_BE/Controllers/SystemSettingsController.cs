using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _systemSettingService;

        public SystemSettingsController(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var settings = await _systemSettingService.GetAllAsync();
            return Ok(settings);
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetByKey(string key)
        {
            var setting = await _systemSettingService.GetByKeyAsync(key);
            if (setting == null) return NotFound();
            return Ok(setting);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromBody] SystemSetting setting)
        {
            var existing = await _systemSettingService.GetByKeyAsync(setting.SettingKey);
            if (existing == null)
            {
                await _systemSettingService.AddAsync(setting);
            }
            else
            {
                await _systemSettingService.UpdateAsync(setting);
            }
            return Ok(new { message = "Setting updated successfully" });
        }

        [HttpPut("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkUpdate([FromBody] List<SystemSetting> settings)
        {
            foreach (var setting in settings)
            {
                var existing = await _systemSettingService.GetByKeyAsync(setting.SettingKey);
                if (existing == null)
                {
                    await _systemSettingService.AddAsync(setting);
                }
                else
                {
                    await _systemSettingService.UpdateAsync(setting);
                }
            }
            return Ok(new { message = "All settings updated successfully" });
        }
    }
}
