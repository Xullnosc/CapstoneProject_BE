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
        /// GET /api/thesis?status=Reviewing&amp;userId=5&amp;searchTitle=...
        /// Returns filtered list of all theses. All query params are optional.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTheses([FromQuery] string? status, [FromQuery] int? userId, [FromQuery] string? searchTitle)
        {
            try
            {
                var theses = await _thesisService.GetFilteredThesesAsync(status, userId, searchTitle);
                return Ok(theses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis/{id}/review
        /// Reviewer only: set thesis evaluation (Pass → Published, Fail → Rejected, or Need Update).
        /// MUST be declared before PUT "{id}" so that /review is matched correctly.
        /// </summary>
        [HttpPut("{id}/review")]
        [Authorize(Policy = "Reviewer")]
        public async Task<IActionResult> ReviewThesis(string id, [FromBody] ReviewThesisDTO dto)
        {
            try
            {
                var allowed = new[] { "Published", "Rejected", "Need Update" };
                if (string.IsNullOrWhiteSpace(dto?.Status) || !allowed.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new { Message = "Status must be one of: Published, Rejected, Need Update." });

                await _thesisService.UpdateThesisStatusAsync(id, dto.Status);
                return Ok(new { Message = "Thesis evaluation updated.", Status = dto.Status });
            }
            catch (Exception ex) when (ex.Message?.Contains("not found") == true)
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
