using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        [Authorize(Roles = "HOD,Admin,Head of Department,Lecturer")]
        public async Task<ActionResult<DashboardStatsDTO>> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving dashboard statistics.", details = ex.Message });
            }
        }

        /// <summary>
        /// Returns aggregated system-wide statistics for the Admin dashboard.
        /// Includes user/role distribution, thesis statuses, semester comparisons,
        /// login trends (6 months), and team statuses across all campuses.
        /// </summary>
        [HttpGet("admin-stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AdminDashboardStatsDTO>> GetAdminDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetAdminDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving admin dashboard statistics.", details = ex.Message });
            }
        }

        [HttpGet("lecturer-stats")]
        [Authorize(Roles = "Lecturer,HOD")]
        public async Task<ActionResult<LecturerDashboardStatsDTO>> GetLecturerDashboardStats()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }

                var isReviewer = string.Equals(
                    User.FindFirst("IsReviewer")?.Value,
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );

                var stats = await _dashboardService.GetLecturerDashboardStatsAsync(userId, isReviewer);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving lecturer dashboard statistics.", details = ex.Message }
                );
            }
        }
    }
}
