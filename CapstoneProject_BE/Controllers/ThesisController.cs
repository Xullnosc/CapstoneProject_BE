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
        /// GET /api/thesis/{id}/review-status
        /// Returns reviewer progress + (optional) HOD final decision.
        /// This is used to show "who has reviewed" while still in-progress.
        /// </summary>
        [HttpGet("{id}/review-status")]
        public async Task<IActionResult> GetReviewStatus(string id)
        {
            try
            {
                var status = await _thesisService.GetReviewStatusAsync(id);
                return Ok(status);
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
        /// PUT /api/thesis/{id}/reviewers
        /// HOD/Admin: assign reviewers for a thesis (2+ reviewers).
        /// </summary>
        [HttpPut("{id}/reviewers")]
        [Authorize(Policy = "HodOrAdmin")]
        public async Task<IActionResult> AssignReviewers(string id, [FromBody] AssignThesisReviewersDTO dto)
        {
            try
            {
                var userId = GetUserId();
                if (dto?.ReviewerIds == null || dto.ReviewerIds.Length == 0)
                    return BadRequest(new { Message = "ReviewerIds is required." });

                var status = await _thesisService.AssignReviewersAsync(id, dto.ReviewerIds, userId);
                return Ok(status);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
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
        /// PUT /api/thesis/{id}/review
        /// Reviewer only: submit reviewer decision (Pass/Fail). If Fail, note is required.
        /// Auto-finalize when all reviewers agree. If split, require HOD decision.
        /// MUST be declared before PUT "{id}" so that /review is matched correctly.
        /// </summary>
        [HttpPut("{id}/review")]
        [Authorize] // temporarily allow any authenticated user; policy-level checks moved into service logic
        public async Task<IActionResult> SubmitReviewerDecision(string id, [FromBody] SubmitThesisDecisionDTO dto)
        {
            try
            {
                var userId = GetUserId();
                Console.WriteLine($"[ThesisReview] SubmitReviewerDecision START - UserId={userId}, ThesisId={id}, Decision={dto?.Decision}");
                var status = await _thesisService.SubmitReviewerDecisionAsync(id, userId, dto);
                Console.WriteLine($"[ThesisReview] SubmitReviewerDecision OK - UserId={userId}, ThesisId={id}, OverallStatus={status.OverallStatus}");
                return Ok(status);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[ThesisReview] 403 UnauthorizedAccessException - {ex.Message}");
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"[ThesisReview] 400 ArgumentException - {ex.Message}");
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"[ThesisReview] 404 KeyNotFoundException - {ex.Message}");
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThesisReview] 400 Exception - {ex}");
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/thesis/{id}/hod-decision
        /// HOD: submit final decision when reviewers are split (1 pass / 1 fail).
        /// If Fail, note is required.
        /// </summary>
        [HttpPut("{id}/hod-decision")]
        [Authorize(Policy = "HodOrAdmin")]
        public async Task<IActionResult> SubmitHodDecision(string id, [FromBody] SubmitThesisDecisionDTO dto)
        {
            try
            {
                var userId = GetUserId();
                var status = await _thesisService.SubmitHodDecisionAsync(id, userId, dto);
                return Ok(status);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
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

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
                throw new UnauthorizedAccessException("User id claim not found in token.");
            return userId;
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
