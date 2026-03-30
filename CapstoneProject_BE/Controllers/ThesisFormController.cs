using BusinessObjects.DTOs;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/thesis-forms")]
    [ApiController]
    [Authorize]
    public class ThesisFormController : ControllerBase
    {
        private readonly IThesisFormService _thesisFormService;

        public ThesisFormController(IThesisFormService thesisFormService)
        {
            _thesisFormService = thesisFormService;
        }

        /// <summary>
        /// POST /api/thesis-forms
        /// Uploads a new Thesis Form (versioned globally). Head of Department only.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadThesisForm([FromForm] UploadThesisFormDTO req)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                    return Unauthorized(new { Message = "Email claim not found in token." });

                var result = await _thesisFormService.UploadThesisFormAsync(req, emailClaim.Value);
                return Ok(new { Message = "Thesis form uploaded successfully.", Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { Message = errorMessage });
            }
        }

        /// <summary>
        /// GET /api/thesis-forms/latest
        /// Gets the latest Thesis Form globally.
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestThesisForm()
        {
            try
            {
                var result = await _thesisFormService.GetLatestFormAsync();
                if (result == null)
                    return NotFound(new { Message = "No thesis form has been uploaded yet." });

                return Ok(new { Message = "Thesis form retrieved successfully.", Data = result });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { Message = errorMessage });
            }
        }

        /// <summary>
        /// GET /api/thesis-forms/histories
        /// Gets the history of all uploaded thesis forms globally.
        /// </summary>
        [HttpGet("histories")]
        public async Task<IActionResult> GetThesisFormHistories()
        {
            try
            {
                var result = await _thesisFormService.GetFormHistoriesAsync();
                return Ok(new { Message = "Thesis form histories retrieved successfully.", Data = result });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { Message = errorMessage });
            }
        }
    }
}
