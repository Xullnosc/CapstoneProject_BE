using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.DTOs;

namespace CapstoneProject_BE.Controllers
{
    [ApiController]
    [Route("api/thesis-applications")]
    [Authorize]
    public class ThesisApplicationController : ControllerBase
    {
        private readonly IThesisApplicationService _service;

        public ThesisApplicationController(IThesisApplicationService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST /api/thesis-applications
        /// Team leader submits a thesis application.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationDTO dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _service.SubmitApplicationAsync(userId, dto.ThesisId);
                return Ok(new { Message = "Application submitted successfully.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis-applications/{id}/cancel
        /// Team leader cancels a Pending application (soft-delete → Cancelled).
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelApplication(int id)
        {
            try
            {
                var userId = GetUserId();
                await _service.CancelApplicationAsync(userId, id);
                return Ok(new { Message = "Application cancelled successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// GET /api/thesis-applications?teamId=...
        /// Get list of applications by team. If teamId is not provided, uses current user's team.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApplications([FromQuery] int? teamId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _service.GetApplicationsByTeamAsync(userId, teamId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// GET /api/thesis-applications/by-thesis?thesisId=...&status=...&search=...&page=...&limit=...
        /// Lecturer gets paginated applications for their thesis.
        /// </summary>
        [HttpGet("by-thesis")]
        public async Task<IActionResult> GetApplicationsByThesis(
            [FromQuery] string thesisId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetUserId();
                var result = await _service.GetApplicationsByThesisAsync(userId, thesisId, status, search, page, limit);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis-applications/{id}/approve
        /// Lecturer approves a Pending application.
        /// </summary>
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            try
            {
                var userId = GetUserId();
                await _service.ApproveApplicationAsync(userId, id);
                return Ok(new { Message = "Application approved successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis-applications/{id}/reject
        /// Lecturer rejects a Pending application.
        /// </summary>
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectApplication(int id)
        {
            try
            {
                var userId = GetUserId();
                await _service.RejectApplicationAsync(userId, id);
                return Ok(new { Message = "Application rejected successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
                throw new UnauthorizedAccessException("User id claim not found in token.");
            return userId;
        }
    }
}
