using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [ApiController]
    [Route("api/system/error-logs")]
    [Authorize(Policy = "HodOrAdmin")]
    public class SystemErrorLogController : ControllerBase
    {
        private readonly ISystemErrorLogService _service;

        public SystemErrorLogController(ISystemErrorLogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? level = null)
        {
            try
            {
                var (logs, totalCount) = await _service.GetLogsAsync(pageNumber, pageSize, level);
                return Ok(new
                {
                    data = logs,
                    totalCount,
                    pageNumber,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving logs", details = ex.Message });
            }
        }
    }
}
