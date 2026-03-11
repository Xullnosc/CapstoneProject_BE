using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.DTOs;

namespace capstone_be.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = BusinessObjects.CampusConstants.Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAccessLogService _accessLogService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, IAccessLogService accessLogService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _accessLogService = accessLogService;
        _logger = logger;
    }

    [HttpGet("hod")]
    public async Task<IActionResult> GetHodAccounts([FromQuery] string? search)
    {
        try
        {
            var result = await _adminService.GetHodAccountsAsync(search);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching HOD accounts");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("hod")]
    public async Task<IActionResult> CreateOrUpdateHod([FromBody] CreateOrUpdateHodDTO dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is null" });
            await _adminService.CreateOrUpdateHodAsync(dto);
            return Ok(new { message = "HOD account created or updated successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating HOD");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("access-logs")]
    public async Task<IActionResult> GetAccessLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var (logs, totalCount) = await _accessLogService.GetPaginatedLogsAsync(page, pageSize);
            return Ok(new
            {
                Data = logs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching access logs");
            return StatusCode(500, new { message = ex.Message });
        }
    }

}
