using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly ISystemParameterService _systemParameterService;

        public SystemController(ISystemParameterService systemParameterService)
        {
            _systemParameterService = systemParameterService;
        }

        [HttpGet("params")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemParameters()
        {
            try
            {
                var parameters = await _systemParameterService.GetAllParametersAsync();
                return Ok(parameters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving system parameters.", details = ex.Message });
            }
        }
        
        [HttpPut("params/{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSystemParameter(string key, [FromBody] SystemParameterDTO updateDto)
        {
            if (string.IsNullOrEmpty(key) || updateDto == null || key != updateDto.Key)
            {
                return BadRequest(new { message = "Invalid input data." });
            }
            
            try
            {
                var existingParam = await _systemParameterService.GetParameterByKeyAsync(key);
                if (existingParam == null)
                {
                    return NotFound(new { message = "System parameter not found." });
                }

                await _systemParameterService.UpdateParameterAsync(updateDto);
                return Ok(new { message = "System parameter updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating system parameter.", details = ex.Message });
            }
        }
    }
}
