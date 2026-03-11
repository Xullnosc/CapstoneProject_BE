using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ThesisController : ControllerBase
    {
        private readonly IThesisService _thesisService;

        public ThesisController(IThesisService thesisService)
        {
            _thesisService = thesisService;
        }

        // ─── Existing Endpoint (untouched) ───────────────────────────────────────

        [HttpPost("propose")]
        public async Task<IActionResult> ProposeThesis([FromForm] ProposeThesisDTO req)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var email = emailClaim.Value;
                var thesis = await _thesisService.ProposeThesisAsync(req, email);
                return Ok(new { Message = "Thesis proposed successfully", Data = thesis });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { Message = errorMessage });
            }
        }

        // ─── Phase 02: New Endpoints ─────────────────────────────────────────────

        /// <summary>
        /// GET /api/thesis/my
        /// Returns all theses owned by the currently logged-in user.
        /// IMPORTANT: must be declared BEFORE /{id} to avoid route conflict.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyTheses([FromQuery] string? status, [FromQuery] string? searchTitle)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var theses = await _thesisService.GetMyThesesAsync(emailClaim.Value, status, searchTitle);
                return Ok(theses);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// GET /api/thesis/{id}
        /// Returns thesis detail including version history.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetThesisDetail(string id)
        {
            try
            {
                var thesis = await _thesisService.GetThesisDetailAsync(id);
                if (thesis == null)
                    return NotFound(new { Message = $"Thesis with id '{id}' not found." });

                return Ok(thesis);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// GET /api/thesis?status=Reviewing&amp;userId=5&amp;searchTitle=...&amp;isLocked=false&amp;lecturerOnly=true
        /// Returns filtered list of all theses. All query params are optional.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTheses([FromQuery] string? status, [FromQuery] int? userId, [FromQuery] string? searchTitle, [FromQuery] int? semesterId, [FromQuery] bool? isLocked, [FromQuery] bool lecturerOnly = false)
        {
            try
            {
                int? excludeUserId = null;
                var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
                var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                
                // If it's a lecturer/reviewer, exclude their own proposals from the list
                if (roleClaim?.Value == BusinessObjects.CampusConstants.Roles.Lecturer && nameIdClaim != null)
                {
                    if (int.TryParse(nameIdClaim.Value, out int currentUserId))
                    {
                        excludeUserId = currentUserId;
                    }
                }

                var theses = await _thesisService.GetFilteredThesesAsync(status, userId, searchTitle, semesterId, isLocked, lecturerOnly, excludeUserId);
                return Ok(theses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis/{id}/review
        /// Reviewer only: set thesis evaluation (Approve or Reject).
        /// Supporting file and comment can be provided.
        /// </summary>
        [HttpPut("{id}/review")]
        [Authorize(Policy = "Reviewer")]
        public async Task<IActionResult> ReviewThesis(string id, [FromForm] ReviewSubmissionDTO dto)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var allowed = new[] { "Approve", "Reject" };
                if (string.IsNullOrWhiteSpace(dto?.Status) || !allowed.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new { Message = "Status must be one of: Approve, Reject." });

                if (dto.Status == "Reject" && string.IsNullOrWhiteSpace(dto.Comment))
                    return BadRequest(new { Message = "Comment is required when rejecting." });

                var updated = await _thesisService.SubmitReviewAsync(id, dto, emailClaim.Value);
                return Ok(new { Message = "Thesis evaluation submitted.", Data = updated });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis/{id}/lock
        /// Lecturer only: toggle the locked/unlocked state of their own thesis.
        /// MUST be declared before PUT "{id}" to avoid route conflict.
        /// </summary>
        [HttpPut("{id}/lock")]
        public async Task<IActionResult> ToggleLockThesis(string id)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var updated = await _thesisService.ToggleThesisLockAsync(id, emailClaim.Value);
                var lockState = updated.IsLocked ? "locked" : "unlocked";
                return Ok(new { Message = $"Thesis {lockState} successfully.", Data = updated });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis/{id}
        /// Updates an existing thesis (upload new file version, update title/description).
        /// Only the owner can update their own thesis.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateThesis(string id, [FromForm] UpdateThesisDTO req)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var updated = await _thesisService.UpdateThesisAsync(id, req, emailClaim.Value);
                return Ok(new { Message = "Thesis updated successfully.", Data = updated });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis/{id}/cancel
        /// Cancel an existing thesis proposal.
        /// Only the owner can cancel their own thesis.
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelThesis(string id)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var updated = await _thesisService.CancelThesisAsync(id, emailClaim.Value);
                return Ok(new { Message = "Thesis cancelled successfully.", Data = updated });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
    }
}
